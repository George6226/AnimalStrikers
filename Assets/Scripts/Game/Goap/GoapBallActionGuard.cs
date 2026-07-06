/// <summary>
/// パス・シュートのコルーチン実行中は GOAP 再計画や二重起動を抑止する。
/// </summary>
public static class GoapBallActionGuard
{
    public static bool IsPassInProgress(AnimalFacade facade)
    {
        if (facade == null)
        {
            return false;
        }

        var pass = facade.GetComponentInChildren<AnimalAction_Pass>(true);
        return pass != null && pass.IsPassInProgress;
    }

    public static bool IsShootInProgress(AnimalFacade facade)
    {
        if (facade == null)
        {
            return false;
        }

        var shoot = facade.GetComponentInChildren<AnimalAction_Shoot>(true);
        return shoot != null && shoot.IsShootInProgress;
    }

    public static bool IsAnyInProgress(AnimalFacade facade) =>
        IsPassInProgress(facade) || IsShootInProgress(facade);

    public static bool IsCommittedGoapAction(GoapActionRuntime action) =>
        action is PassToTeammateActionRuntime || action is ShootAtGoalActionRuntime;
}
