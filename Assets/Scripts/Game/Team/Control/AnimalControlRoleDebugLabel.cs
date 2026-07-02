using Cinemachine;
using TMPro;
using UnityEngine;

/// <summary>
/// 段階0デバッグ: 頭上にロール・編成・GOAP・簡易移動の状態を表示する。
/// </summary>
[RequireComponent(typeof(AnimalControlAssignment))]
public class AnimalControlRoleDebugLabel : MonoBehaviour
{
    private const string LabelChildName = "ControlRoleDebugLabel";
    private const string FontResourcePath = "Fonts & Materials/LiberationSans SDF";
    private const string MaterialResourcePath = "Fonts & Materials/LiberationSans SDF - Overlay";

    [Header("表示")]
    [SerializeField] private AnimalControlAssignment _assignment;
    [SerializeField] private bool _visible = true;
    [SerializeField] private Vector3 _localOffset = new Vector3(0f, 2.9f, 0f);
    [SerializeField] private float _fontSize = 8f;
    [SerializeField] private float _outlineWidth = 0.25f;

    [Header("段階0 デバッグ項目")]
    [SerializeField] private bool _showFormationSlot = true;
    [SerializeField] private bool _showTacticalRole = true;
    [SerializeField] private bool _showGoapState = true;
    [SerializeField] private bool _showMovementBrainState = true;

    private TextMeshPro _label;
    private Transform _labelTransform;
    private AnimalFormationSlot _formationSlot;
    private GoapAgent _goapAgent;
    private TeammateNpcMovementBrain _movementBrain;
    private string _lastRenderedText = string.Empty;
    private float _baselineLabelPitchX;
    private bool _hasBaselineLabelPitchX;

    public string LastRenderedText => _lastRenderedText;
    public bool IsLabelVisible => _label != null && _label.enabled;

    private void Awake()
    {
        if (_assignment == null)
        {
            _assignment = GetComponent<AnimalControlAssignment>();
        }

        _formationSlot = GetComponent<AnimalFormationSlot>();
        _goapAgent = AnimalGoapBrainComponents.Resolve(gameObject).Agent;
        _movementBrain = GetComponent<TeammateNpcMovementBrain>();
        EnsureLabel();
    }

    private void OnEnable()
    {
        if (_assignment != null)
        {
            _assignment.RoleChanged += OnRoleChanged;
        }
    }

    private void OnDisable()
    {
        if (_assignment != null)
        {
            _assignment.RoleChanged -= OnRoleChanged;
        }
    }

    private void OnRoleChanged(AnimalControlRole role)
    {
        RefreshDisplay();
    }

    private void LateUpdate()
    {
        if (!AnimalDebugOverlay.Enabled || !_visible)
        {
            if (_label != null)
            {
                _label.enabled = false;
            }

            return;
        }

        if (!TryBindLabelReferences())
        {
            EnsureLabel();
        }

        if (_labelTransform == null)
        {
            return;
        }

        _labelTransform.localPosition = _localOffset;
        FaceCamera();
        RefreshDisplay();
    }

    private void FaceCamera()
    {
        var cam = GetViewCamera();
        if (cam == null)
        {
            return;
        }

        TryCaptureBaselineLabelPitchX(cam);

        // Yawは現在のカメラに同期。ラベルXは起動時カメラX（ピッチ）を初期角度として固定。
        float yaw = cam.transform.eulerAngles.y;
        _labelTransform.rotation = Quaternion.Euler(_baselineLabelPitchX, yaw, 0f);
    }

    private void TryCaptureBaselineLabelPitchX(Camera cam)
    {
        if (_hasBaselineLabelPitchX || cam == null)
        {
            return;
        }

        _baselineLabelPitchX = cam.transform.eulerAngles.x;
        _hasBaselineLabelPitchX = true;
    }

    private static Camera GetViewCamera()
    {
        if (Camera.main != null)
        {
            return Camera.main;
        }

        var brain = Object.FindObjectOfType<CinemachineBrain>();
        return brain != null ? brain.OutputCamera : null;
    }

    private bool TryBindLabelReferences()
    {
        if (_label != null && !IsOurLabelObject(_label.transform))
        {
            _label = null;
        }

        if (_label != null && _labelTransform == null)
        {
            _labelTransform = _label.transform;
        }

        if (_labelTransform == null)
        {
            Transform existing = transform.Find(LabelChildName);
            if (existing != null)
            {
                _labelTransform = existing;
                _label = existing.GetComponent<TextMeshPro>();
            }
        }

        return _labelTransform != null && _label != null;
    }

    private bool IsOurLabelObject(Transform t)
    {
        return t != null && t.parent == transform && t.name == LabelChildName;
    }

    private void EnsureLabel()
    {
        if (TryBindLabelReferences())
        {
            return;
        }

        var go = new GameObject(LabelChildName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = _localOffset;
        _labelTransform = go.transform;

        _label = go.AddComponent<TextMeshPro>();
        ApplyLabelStyle();
        RefreshDisplay();
    }

    private void ApplyLabelStyle()
    {
        if (_label == null)
        {
            return;
        }

        _label.fontSize = _fontSize;
        _label.alignment = TextAlignmentOptions.Center;
        _label.textWrappingMode = TextWrappingModes.NoWrap;
        _label.overflowMode = TextOverflowModes.Overflow;
        _label.rectTransform.sizeDelta = new Vector2(8f, 3.2f);
        _label.enableAutoSizing = false;
        _label.lineSpacing = -4f;
        _label.outlineWidth = _outlineWidth;
        _label.outlineColor = new Color(0.05f, 0.05f, 0.1f, 0.9f);

        var font = Resources.Load<TMP_FontAsset>(FontResourcePath);
        if (font != null)
        {
            _label.font = font;
        }

        var material = Resources.Load<Material>(MaterialResourcePath);
        if (material != null)
        {
            _label.fontSharedMaterial = material;
        }
    }

    private void RefreshDisplay()
    {
        EnsureLabel();
        if (_label == null || _assignment == null)
        {
            return;
        }

        AnimalControlRole role = _assignment.Role;
        bool shouldShow = _visible && AnimalDebugOverlay.Enabled && role != AnimalControlRole.Unassigned;
        _label.enabled = shouldShow;
        if (!shouldShow)
        {
            _label.text = string.Empty;
            _lastRenderedText = string.Empty;
            return;
        }

        _label.text = BuildLabelText(role);
        _lastRenderedText = _label.text;
        _label.color = GetRoleColor(role);
    }

    private string BuildLabelText(AnimalControlRole role)
    {
        var lines = new System.Collections.Generic.List<string> { GetIdentityLine(role) };

        // 固定2行目: G:<Goal>（GOAP未使用時も "-" を表示）
        string goal = _showGoapState ? GetGoapGoalLine() : "-";
        lines.Add($"G:{goal}");

        // 固定3行目: A:<Action>（"-" や FB:* もそのまま表示）
        string action = GetActionLine(role);
        lines.Add($"A:{action}");

        return string.Join("\n", lines);
    }

    private static bool ShouldSplitGoalAndAction(string goal, string action)
    {
        if (goal == "-" || action == "-" || action == "Planning" || action == "PlanFailed")
        {
            return false;
        }

        if (action.StartsWith("Queued"))
        {
            return false;
        }

        return goal != action && (goal.Length + action.Length) > 18;
    }

    /// <summary>1行目: ロール + ポジション（例: NPC·LW / MAIN·CF）</summary>
    private string GetIdentityLine(AnimalControlRole role)
    {
        if (GoapMainNpcVerifyEnvironment.IsActive && role == AnimalControlRole.TeammateNpc)
        {
            var facade = GetComponent<AnimalFacade>();
            string tierTag = GoapMainNpcVerifyEnvironment.ResolveTier(facade) == GoapNpcTier.Main
                ? "MAIN"
                : "SUB";
            string position = GetPositionTag();
            if (string.IsNullOrEmpty(position) || position == tierTag)
            {
                return tierTag;
            }

            return $"{tierTag}·{position}";
        }

        if (GoapMainNpcProductionEnvironment.IsActive && role == AnimalControlRole.Human)
        {
            var facade = GetComponent<AnimalFacade>();
            var goap = AnimalGoapBrainComponents.Resolve(facade);
            bool m2Active = goap.Agent != null
                && goap.Agent.enabled
                && GoapMainNpcProductionEnvironment.ShouldEnableGoap(goap.Blackboard, facade);
            string humanTag = m2Active ? "YOU·M2" : "YOU";
            string humanPosition = GetPositionTag();
            if (string.IsNullOrEmpty(humanPosition))
            {
                return humanTag;
            }

            return $"{humanTag}·{humanPosition}";
        }

        string roleTag = role switch
        {
            AnimalControlRole.Human => "YOU",
            AnimalControlRole.TeammateNpc => "NPC",
            AnimalControlRole.GoalkeeperNpc => "GK",
            _ => "?",
        };

        string positionTag = GetPositionTag();
        if (string.IsNullOrEmpty(positionTag) || positionTag == roleTag)
        {
            return roleTag;
        }

        return $"{roleTag}·{positionTag}";
    }

    private string GetPositionTag()
    {
        if (_formationSlot == null || !_formationSlot.IsAssigned)
        {
            return _showTacticalRole ? "—" : string.Empty;
        }

        int index = _formationSlot.Index;
        if (_showTacticalRole)
        {
            return FormationTacticalRole.GetShortName(index);
        }

        if (_showFormationSlot)
        {
            return $"S{index}";
        }

        return string.Empty;
    }

    /// <summary>2行目: 行動の要約（GOAPまたは簡易移動）。短いときはゴール›アクション1行。</summary>
    private string GetBehaviorLine(AnimalControlRole role)
    {
        if (!_showGoapState && !_showMovementBrainState)
        {
            return string.Empty;
        }

        string goal = _showGoapState ? GetGoapGoalLine() : "-";
        string action = GetActionLine(role);

        if (goal == "-" && (action == "-" || string.IsNullOrEmpty(action)))
        {
            return role == AnimalControlRole.TeammateNpc ? "—" : string.Empty;
        }

        if (!_showGoapState)
        {
            return action;
        }

        if (goal == "-" || goal == action)
        {
            return action;
        }

        if (action == "-" || action == "Planning" || action == "PlanFailed")
        {
            return goal;
        }

        return $"{goal} › {action}";
    }

    private string GetGoapGoalLine()
    {
        if (_goapAgent == null)
        {
            _goapAgent = AnimalGoapBrainComponents.Resolve(gameObject).Agent;
        }

        if (_goapAgent == null || !_goapAgent.enabled)
        {
            return "-";
        }

        return _goapAgent.DebugCurrentGoalName;
    }

    private string GetActionLine(AnimalControlRole role)
    {
        if (_goapAgent == null)
        {
            _goapAgent = AnimalGoapBrainComponents.Resolve(gameObject).Agent;
        }

        if (_goapAgent != null && _goapAgent.enabled)
        {
            string goapAction = _goapAgent.DebugCurrentActionName;
            string fallback = GetMovementBrainLine(role);
            if ((goapAction == "-" || goapAction == "PlanFailed") && !string.IsNullOrEmpty(fallback))
            {
                return $"FB:{fallback}";
            }

            return goapAction;
        }

        string movement = GetMovementBrainLine(role);
        return !string.IsNullOrEmpty(movement) ? movement : "-";
    }

    private string GetMovementBrainLine(AnimalControlRole role)
    {
        if (role != AnimalControlRole.TeammateNpc)
        {
            return string.Empty;
        }

        if (_movementBrain == null)
        {
            _movementBrain = GetComponent<TeammateNpcMovementBrain>();
        }

        if (_movementBrain == null || !_movementBrain.enabled)
        {
            return string.Empty;
        }

        if (_goapAgent != null && _goapAgent.enabled && _goapAgent.IsActivelyControlling)
        {
            return string.Empty;
        }

        return _movementBrain.CurrentTacticalModeLabel;
    }

    private static Color GetRoleColor(AnimalControlRole role)
    {
        return role switch
        {
            // 濃い青（薄い水色は芝で見えにくい）
            AnimalControlRole.Human => new Color(0.12f, 0.38f, 0.92f),
            // マゼンタ系（緑フィールドと被らない）
            AnimalControlRole.TeammateNpc => new Color(1f, 0.45f, 0.95f),
            // ゴールド
            AnimalControlRole.GoalkeeperNpc => new Color(1f, 0.82f, 0.35f),
            _ => new Color(0.92f, 0.92f, 0.95f),
        };
    }
}
