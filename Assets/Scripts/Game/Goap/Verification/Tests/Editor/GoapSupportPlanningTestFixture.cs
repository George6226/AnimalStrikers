using System;
using System.Collections.Generic;
using System.Reflection;
using Photon.Pun;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TeammateNpcSupportPlanning の幾何判定を EditMode で検証するための最小シーン構成。
/// </summary>
public sealed class GoapSupportPlanningTestFixture : IDisposable
{
    private const int PhotonViewIdBase = 9100;

    private readonly GameObject _root;
    private readonly TeamFacade _teamFacade;
    private readonly TeamBlackboard _teamBlackboard;
    private readonly TeamRegistar _teamRegistar;
    private readonly GoapSupportLayoutTuning _tuning = new();
    private readonly Dictionary<int, PlayerBlackboard> _slotBlackboards = new();
    private readonly Dictionary<int, int> _slotPhotonViewIds = new();

    public TeamBlackboard TeamBlackboard => _teamBlackboard;
    public GoapSupportLayoutTuning Tuning => _tuning;

    public GoapSupportPlanningTestFixture()
    {
        _root = new GameObject("GoapSupportPlanningTestRoot");

        _teamFacade = _root.AddComponent<TeamFacade>();
        _teamRegistar = _root.AddComponent<TeamRegistar>();
        _teamBlackboard = _root.AddComponent<TeamBlackboard>();

        SetPrivateField(_teamFacade, "_teamRegistar", _teamRegistar);
        SetPrivateField(_teamFacade, "_teamBlackboard", _teamBlackboard);
        SetStaticField(typeof(TeamFacade), "_instance", _teamFacade);

        _teamBlackboard.FieldInfo.Initialize(ConstData.FIELD_SIZE_Z, ConstData.FIELD_SIZE_X);
        _teamBlackboard.BallInfo.Initialize();
        _teamBlackboard.BallInfo.setExistBall();

        for (int slot = 0; slot <= 2; slot++)
        {
            CreateAlly(slot);
        }

        TeammateNpcGoapRoleDifferentiation.Enabled = true;
        TeammateNpcSupportPlanning.SetVerificationOnlySupportAction(GoapSupportActionUnderTest.None);
    }

    public PlayerBlackboard GetBlackboard(int slot)
    {
        if (!_slotBlackboards.TryGetValue(slot, out PlayerBlackboard bb))
        {
            throw new ArgumentOutOfRangeException(nameof(slot), slot, "slot must be 0..2");
        }

        return bb;
    }

    public bool TryGetFieldContext(out GoapSupportLayoutFieldContext ctx) =>
        GoapSupportLayoutPatternLibrary.TryGetFieldContext(_tuning, out ctx);

    public void ApplyPattern(GoapSupportLayoutPatternId pattern)
    {
        if (!TryGetFieldContext(out GoapSupportLayoutFieldContext ctx))
        {
            throw new InvalidOperationException("Field context unavailable.");
        }

        Dictionary<int, Vector3> targets = GoapSupportLayoutPatternLibrary.ComputeTargets(pattern, ctx, _tuning);
        int ballOwnerSlot = GoapSupportLayoutPatternLibrary.GetBallOwnerSlotForPattern(pattern, 0);

        foreach (KeyValuePair<int, Vector3> entry in targets)
        {
            SetSlotPosition(entry.Key, entry.Value);
        }

        if (!targets.TryGetValue(ballOwnerSlot, out Vector3 ownerPos))
        {
            ownerPos = GoapSupportLayoutPatternLibrary.ResolvePatternOwnerPosition(pattern, ctx, _tuning);
        }

        ConfigureTeamBallAttack(ballOwnerSlot, ownerPos);
    }

    public void SetSlotPosition(int slot, Vector3 position)
    {
        PlayerBlackboard bb = GetBlackboard(slot);
        bb.BasicData.Self.transform.position = position;
        bb.PhysicalState.updatePhysicalInfo(position, Vector3.zero);
    }

    public void ConfigureTeamBallAttack(int ballOwnerSlot, Vector3 ownerPosition)
    {
        if (!_slotPhotonViewIds.TryGetValue(ballOwnerSlot, out int ownerViewId))
        {
            throw new ArgumentOutOfRangeException(nameof(ballOwnerSlot), ballOwnerSlot, "slot must be 0..2");
        }

        _teamBlackboard.BallInfo.updateBallID(
            ownerViewId,
            BallManager_State.BELONG_TEAM.PLAYER,
            ownerPosition);
        _teamBlackboard.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);
        _teamBlackboard.BallInfo.updateBallOwnerPosition(ownerPosition);
    }

    public void Dispose()
    {
        TeammateNpcSupportPlanning.SetVerificationOnlySupportAction(GoapSupportActionUnderTest.None);
        TeammateNpcGoapRoleDifferentiation.Enabled = true;
        SetStaticField(typeof(TeamFacade), "_instance", null);

        if (_root != null)
        {
            UnityEngine.Object.DestroyImmediate(_root);
        }
    }

    private void CreateAlly(int slot)
    {
        int viewId = PhotonViewIdBase + slot;
        var allyRoot = new GameObject($"Ally_slot{slot}");
        allyRoot.transform.SetParent(_root.transform, false);

        var photonView = allyRoot.AddComponent<PhotonView>();
        photonView.ViewID = viewId;

        var facade = allyRoot.AddComponent<AnimalFacade>();
        var formationSlot = allyRoot.AddComponent<AnimalFormationSlot>();
        formationSlot.Initialize(slot);

        var assignment = allyRoot.AddComponent<AnimalControlAssignment>();
        assignment.SetRole(AnimalControlRole.TeammateNpc);

        var animalInfo = allyRoot.AddComponent<AnimalInfo>();
        SetPrivateField(animalInfo, "_animalInfo", CreateNonGkAnimalParam());
        SetPrivateField(facade, "_animalInfo", animalInfo);

        var bbGo = new GameObject("PlayerBlackboard");
        bbGo.transform.SetParent(allyRoot.transform, false);
        var bb = bbGo.AddComponent<PlayerBlackboard>();
        bb.BasicData.init(bbGo);
        bb.PhysicalState.init(Vector3.zero);
        bb.BallState.init();
        bb.ActionState.init();

        AddAllyToRegistar(facade);

        _slotBlackboards[slot] = bb;
        _slotPhotonViewIds[slot] = viewId;
    }

    private static Param_AnimalInfo CreateNonGkAnimalParam()
    {
        var param = ScriptableObject.CreateInstance<Param_AnimalInfo>();
        var so = new SerializedObject(param);
        SerializedProperty isGk = so.FindProperty("_animalInfoParam._isGK");
        if (isGk != null)
        {
            isGk.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        return param;
    }

    private void AddAllyToRegistar(AnimalFacade facade)
    {
        FieldInfo field = typeof(TeamRegistar).GetField("_allyList", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field?.GetValue(_teamRegistar) is List<AnimalFacade> allyList)
        {
            allyList.Add(facade);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            throw new MissingFieldException(target.GetType().FullName, fieldName);
        }

        field.SetValue(target, value);
    }

    private static void SetStaticField(Type type, string fieldName, object value)
    {
        FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
        if (field == null)
        {
            throw new MissingFieldException(type.FullName, fieldName);
        }

        field.SetValue(null, value);
    }
}
