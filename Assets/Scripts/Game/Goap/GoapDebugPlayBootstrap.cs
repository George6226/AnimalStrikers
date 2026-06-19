using System;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// GameScene 直 Play 時に Photon の BattleMode 未設定でキャラが生成されない問題を回避する。
/// GoapDebug 配下に配置し、NPC 対戦としてアバター生成を確実に行う。
/// </summary>
[DefaultExecutionOrder(-200)]
public class GoapDebugPlayBootstrap : MonoBehaviour
{
    public static bool IsSpawnReady { get; private set; }
    public static event Action SpawnReady;

    [SerializeField] private PhotonAvatarCreator _avatarCreator;
    [SerializeField] private bool _forceNpcBattleMode = true;
    [SerializeField] private float _connectTimeoutSeconds = 30f;
    [SerializeField] private float _spawnWaitTimeoutSeconds = 90f;
    [SerializeField] private float _spawnCheckIntervalSeconds = 0.25f;

    private float ConnectTimeoutSeconds =>
        GoapBatchVerifyEnvironment.ResolveTimeout(_connectTimeoutSeconds, 60f);

    private float SpawnWaitTimeoutSeconds =>
        GoapBatchVerifyEnvironment.ResolveTimeout(_spawnWaitTimeoutSeconds, 180f);

    private bool _completed;

    private void Awake()
    {
        IsSpawnReady = false;
    }

    private void Start()
    {
        StartCoroutine(EnsureSpawnCoroutine());
    }

    private IEnumerator EnsureSpawnCoroutine()
    {
        if (_avatarCreator == null)
        {
            _avatarCreator = FindFirstObjectByType<PhotonAvatarCreator>();
        }

        if (GoapBatchVerifyEnvironment.IsActive)
        {
            yield return EnsureBatchVerifySpawnCoroutine();
            yield break;
        }

        float connectElapsed = 0f;
        while (connectElapsed < ConnectTimeoutSeconds && !PhotonNetwork.IsConnectedAndReady)
        {
            connectElapsed += Time.deltaTime;
            yield return null;
        }

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Log($"spawn aborted: Photon not connected after {ConnectTimeoutSeconds:F0}s");
            yield break;
        }

        Player local = PhotonNetwork.LocalPlayer;
        if (local == null)
        {
            Log("spawn aborted: LocalPlayer is null");
            yield break;
        }

        if (!PhotonNetwork.InRoom)
        {
            Log("waiting for room join (PhotonRoomMatching)");
            yield return WaitForFieldPlayersSpawned(SpawnWaitTimeoutSeconds);
            CompleteIfReady();
            yield break;
        }

        if (_forceNpcBattleMode && local.getBattleMode() != ConstData.BATTLE_MODE.NPC)
        {
            var previousMode = local.getBattleMode();
            local.setBattleMode(ConstData.BATTLE_MODE.NPC);
            Log($"forced BattleMode=NPC (was {previousMode})");
        }

        if (PhotonPlayerInfo.Instance != null)
        {
            PhotonPlayerInfo.Instance.Initialize(local);
        }

        if (!HasMinimumFieldPlayers())
        {
            if (_avatarCreator != null)
            {
                Log("InRoom but field players missing -> executeAvatarCreator");
                _avatarCreator.executeAvatarCreator();
            }
            else
            {
                Log("InRoom but PhotonAvatarCreator not found");
            }

            yield return WaitForFieldPlayersSpawned(SpawnWaitTimeoutSeconds);
        }

        CompleteIfReady();
    }

    private IEnumerator EnsureBatchVerifySpawnCoroutine()
    {
        Log("batch verify: starting offline Photon session");

        PhotonNetwork.OfflineMode = true;
        PhotonNetwork.IsMessageQueueRunning = true;
        PhotonNetwork.ConnectUsingSettings();

        float connectElapsed = 0f;
        while (connectElapsed < ConnectTimeoutSeconds && !PhotonNetwork.IsConnectedAndReady)
        {
            connectElapsed += Time.deltaTime;
            yield return null;
        }

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Log($"batch verify spawn aborted: Photon offline connect failed after {ConnectTimeoutSeconds:F0}s");
            yield break;
        }

        if (!PhotonNetwork.InRoom)
        {
            PhotonNetwork.CreateRoom(
                "GoapBatchVerify",
                new RoomOptions
                {
                    MaxPlayers = 2,
                    IsVisible = false,
                    IsOpen = false,
                });

            float joinElapsed = 0f;
            while (joinElapsed < SpawnWaitTimeoutSeconds && !PhotonNetwork.InRoom)
            {
                joinElapsed += _spawnCheckIntervalSeconds;
                yield return new WaitForSeconds(_spawnCheckIntervalSeconds);
            }
        }

        if (!PhotonNetwork.InRoom)
        {
            Log("batch verify spawn aborted: offline room join failed");
            yield break;
        }

        Player local = PhotonNetwork.LocalPlayer;
        if (local == null)
        {
            Log("batch verify spawn aborted: LocalPlayer is null");
            yield break;
        }

        if (local.getBattleMode() != ConstData.BATTLE_MODE.NPC)
        {
            local.setBattleMode(ConstData.BATTLE_MODE.NPC);
            Log("batch verify: forced BattleMode=NPC");
        }

        if (PhotonPlayerInfo.Instance != null)
        {
            PhotonPlayerInfo.Instance.Initialize(local);
        }

        if (_avatarCreator == null)
        {
            Log("batch verify spawn aborted: PhotonAvatarCreator not found");
            yield break;
        }

        if (!HasMinimumFieldPlayers())
        {
            Log("batch verify: executeAvatarCreator");
            _avatarCreator.executeAvatarCreator();
            yield return WaitForFieldPlayersSpawned(SpawnWaitTimeoutSeconds);
        }

        CompleteIfReady();
    }

    private IEnumerator WaitForFieldPlayersSpawned(float timeoutSeconds)
    {
        float elapsed = 0f;
        while (elapsed < timeoutSeconds)
        {
            if (HasMinimumFieldPlayers())
            {
                yield break;
            }

            elapsed += _spawnCheckIntervalSeconds;
            yield return new WaitForSeconds(_spawnCheckIntervalSeconds);
        }
    }

    private void CompleteIfReady()
    {
        if (_completed)
        {
            return;
        }

        if (!HasMinimumFieldPlayers())
        {
            Log($"spawn timeout: fieldPlayers={CountFieldPlayers()} photonInRoom={PhotonNetwork.InRoom} " +
                $"battleMode={(PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.getBattleMode().ToString() : "null")}");
            return;
        }

        _completed = true;
        IsSpawnReady = true;
        Log($"spawn ready fieldPlayers={CountFieldPlayers()}");
        TryForceGameStateForBatchVerify();
        SpawnReady?.Invoke();
    }

    private static void TryForceGameStateForBatchVerify()
    {
        if (!GoapBatchVerifyEnvironment.IsActive)
        {
            return;
        }

        if (StateManager.Instance == null)
        {
            Log("batch verify: StateManager missing; GAME state not forced");
            return;
        }

        if (StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME))
        {
            return;
        }

        StateManager.Instance.changeStateLocal(StateManager.STATE_KIND.GAME);
        Log("batch verify: forced GAME state (skip kickoff UI)");
    }

    private static bool HasMinimumFieldPlayers()
    {
        return CountFieldPlayers() >= 3;
    }

    private static int CountFieldPlayers()
    {
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return 0;
        }

        int count = 0;
        foreach (var ally in regist.Allys)
        {
            if (ally != null && !ally.IsGK())
            {
                count++;
            }
        }

        return count;
    }

    private static void Log(string message)
    {
        Debug.Log($"[GoapDebugPlayBootstrap] {message}");
    }
}
