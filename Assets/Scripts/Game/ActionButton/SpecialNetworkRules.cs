/// <summary>
/// 6-E 残: 必殺技の Photon 同期ルール（EditMode 向け純関数）。
/// オーナーはローカルで発動・終了し、他クライアントへゲージリセットと終了処理を伝える。
/// </summary>
public static class SpecialNetworkRules
{
    /// <summary>NPC 単独プレイやルーム外では RPC しない。</summary>
    public static bool ShouldSyncSpecialOverPhoton(ConstData.BATTLE_MODE battleMode, bool inRoom) =>
        inRoom && battleMode != ConstData.BATTLE_MODE.NPC;

    /// <summary>オーナーのみ RPC を送る。</summary>
    public static bool ShouldOwnerBroadcastSpecialRpc(bool photonViewIsMine) => photonViewIsMine;

    /// <summary>RPC 受信側で ExecuteSpecial を再実行しない（アニメ・実装はオーナー＋既存同期）。</summary>
    public static bool ShouldRunExecuteSpecialOnNetworkMirror() => false;

    /// <summary>RPC 受信側でスペシャルゲージを 0 に揃える。</summary>
    public static bool ShouldResetGaugeOnNetworkMirror() => true;
}
