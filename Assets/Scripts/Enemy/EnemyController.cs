using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Linq;

public class EnemyController : MonoBehaviourPunCallbacks
{
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private AnimalAction_Pass passAction;
    [SerializeField] private AnimalAction_Shoot _shootAction;
    private PhotonAvatarContainerChild myCharacter;
    private AnimalComponentManager componentManager;
    private AnimalHandler animalHandler;
    private PhotonView photonView;
    private bool isAttacking = false;

    void Start()
    {
        // if (PhotonPlayerInfo.Instance.BattleMode == ConstData.BATTLE_MODE.NPC)
        // {
        //     StartCoroutine(WaitForEnemy());
        // }
    }

    IEnumerator WaitForEnemy()
    {
        // while (true)
        // {
        //     var enemies = PhotonAvatarContainer.Instance.Enemies;
        //     if (enemies.Count > 0)
        //     {
        //         // GK以外の敵をフィルタリング
        //         var availableEnemies = enemies.Where(enemy => {
        //             var compManager = enemy.GetComponent<AnimalComponentManager>();
        //             if (compManager != null)
        //             {
        //                 return !compManager.IsGK;
        //             }
        //             return false;
        //         }).ToList();

        //         if (availableEnemies.Count > 0)
        //         {
        //             // ランダムな敵を選択（GK以外から）
        //             int randomIndex = Random.Range(0, availableEnemies.Count);
        //             myCharacter = availableEnemies[randomIndex];
                    
        //             // コンポーネントの取得
        //             componentManager = myCharacter.GetComponent<AnimalComponentManager>();
        //             photonView = myCharacter.GetComponent<PhotonView>();
                    
        //             if (componentManager != null)
        //             {
        //                 animalHandler = componentManager.Animal;
        //                 Debug.Log($"選択された敵: {myCharacter.name} (GKか?: {componentManager.IsGK})");
        //                 break; // 敵が見つかったのでループを抜ける
        //             }
        //         }
        //     }

        //     Debug.Log("GK以外の敵を待機中...");
        //     yield return new WaitForSeconds(0.5f);
        // }

        yield break;
    }

    void Update()
    {
        // if (myCharacter == null || photonView == null || !photonView.IsMine || animalHandler == null) return;

        // float moveX = Input.GetAxisRaw("Vertical");
        // float moveZ = Input.GetAxisRaw("Horizontal"); 

        // if (moveX != 0 || moveZ != 0)
        // {
        //     float rad = Mathf.Atan2(moveX, moveZ);
        //     animalHandler.rotate(rad);
        //     animalHandler.move(1.0f, moveSpeed);
        // }
        // else
        // {
        //     animalHandler.stand();
        // }

        // if (Input.GetKeyDown(KeyCode.J))
        // {
        //     if (passAction != null)
        //     {
        //         // passAction.pass(false);
        //     }
        // }

        // if (Input.GetKeyDown(KeyCode.K))
        // {
        //     if (_shootAction != null)
        //     {
        //         _shootAction.shoot(false);  // NPCとしてシュート
        //     }
        // }

        // if (Input.GetKeyDown(KeyCode.Return) && !isAttacking)
        // {
        //     Attack();
        // }
    }

    void Attack()
    {
        // if (animalHandler == null) return;

        // isAttacking = true;
        // animalHandler.attack();
        // Invoke("ResetAttack", 0.5f);
    }

    void ResetAttack()
    {
        // isAttacking = false;
    }
} 