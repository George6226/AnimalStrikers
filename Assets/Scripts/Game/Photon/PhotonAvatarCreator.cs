using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;


// Photonのアバター生成
public class PhotonAvatarCreator : MonoBehaviourPunCallbacks
{
    // 動物リスト/キーパー/味方の位置更新
    [SerializeField] private List<PhotonAvatarContainerChild> _masterAnimals;
    [SerializeField] private List<PhotonAvatarContainerChild> _subAnimals;
    [SerializeField] private PhotonAvatarContainerChild _keeper;
    //[SerializeField] private TeamsPositionUpdator_Allys _allysPosUpdator;

    [SerializeField] private GameObject _roomMatching;

    // 生成完了
    private int _created = 0;
    public int Created
    {
        get { return _created; }
    }

    // プレイヤータイプの定義
    private enum PlayerType
    {
        Master,
        Sub,
        NPC
    }

    void Start()
    {   
        // ルームに入っているか確認
        if (!PhotonNetwork.InRoom){
            // ルームマッチングを行う
            _roomMatching.SetActive(true);
        }
        else{
            // ルームに入っている場合は通常の処理を開始
            StartCoroutine(WaitForPoolAndSpawn());
        }
    }

    // アバターの生成(ルームマッチングから)
    public void executeAvatarCreator()
    {   
        StartCoroutine(WaitForPoolAndSpawn());
    }

    // マッチング後プールからキャラクタを生成
    private IEnumerator WaitForPoolAndSpawn()
    {
        // ネットワークメッセージ処理の有効
        PhotonNetwork.IsMessageQueueRunning = true;
        // プールが設定されるまで待機
        while (PhotonNetwork.PrefabPool == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // NPC対戦
        if (PhotonPlayerInfo.Instance.BattleMode == ConstData.BATTLE_MODE.NPC)
        {
            // マスター = Player
            if (PhotonPlayerInfo.Instance.IsMasterClient)
            {
                Debug.Log("[PhotonAvatarCreator] NPC対戦の場合はプレイヤーがNPCも生成");
                // NPC対戦の場合はプレイヤーがNPCも生成
                updateNickName(PlayerType.Master);
                SpawnCharacters(PlayerType.Master);
                updateNickName(PlayerType.NPC);
                SpawnCharacters(PlayerType.NPC);
            }
        }
        // 対人戦
        else
        {
            // 通常対戦の場合は全プレイヤーの準備を待つ
            while (!PhotonNetwork.CurrentRoom.Players.Values.All(p => p.getIsSceneLoaded())){
                yield return new WaitForSeconds(0.1f);
            }

            // 自分のキャラクターを生成
            if (PhotonPlayerInfo.Instance.IsMasterClient)
            {
                Debug.Log("[PhotonAvatarCreator] 対人戦の場合はプレイヤーが生成");
                updateNickName(PlayerType.Master);
                SpawnCharacters(PlayerType.Master);
            }
            else{
                Debug.Log("[PhotonAvatarCreator] 対人戦の場合は相手が生成");
                // 方向を逆に
                //_allysPosUpdator.AllyDir = false;
                updateNickName(PlayerType.Sub);
                SpawnCharacters(PlayerType.Sub);
            }
        }
    }

    // キャラクタの生成
    private void SpawnCharacters(PlayerType playerType)
    {
        // マスターか?
        bool isMaster = playerType == PlayerType.Master;
        bool isNPC = playerType == PlayerType.NPC;
        
        // データを読み込む（共通化）
        // LoadCharacterData内でデータがない場合は、_masterAnimals/_subAnimalsからtypeを取得し、getCharacterPositionから位置を取得
        var characterData = LoadCharacterData(playerType);
        List<Vector3> characterPositions = characterData.positions;
        List<Param_AnimalInfo.AnimalType> characterTypes = characterData.types;
        
        // ユニフォームの種類を0~4のランダムに設定
        int uniformKind = UnityEngine.Random.Range(0, 5);
        
        // SpawnCharactersWithPositionsを呼び出し（LoadCharacterDataでデフォルトデータも設定済み）
        GameObject ownerAnimal = SpawnCharactersWithPositions(playerType, characterPositions, characterTypes, uniformKind);

        // マスターのみボール生成
        if(isMaster)
        {            
            Vector3 ballPos = ES3.Load<Vector3>(DataKey.DATAKEY_GAME_INFO + DataKey.VECTOR3_BALL_POSITION, Vector3.zero);
            var ball = PhotonNetwork.InstantiateRoomObject("Ball_Test", ballPos, Quaternion.identity, 0);
        }

        // キャラクター生成完了をプロパティで通知
        PhotonNetwork.LocalPlayer.setIsCharacterSpawned(true);
        // 生成数を追加
        _created++;
    }
    // スコアのニックネームの更新
    private void updateNickName(PlayerType playerType)
    {
        // NPCの場合
        if (playerType == PlayerType.NPC){
            ScoreManager.Instance.SetPlayerName("NPC");
        }
        // プレイヤーを表示
        else{
            ScoreManager.Instance.SetPlayerName(PhotonPlayerInfo.Instance.PlayerName);
        }
    }

    /// <summary>
    /// キャラクターデータを読み込む（共通化）
    /// MasterとSubはPlayer側から、NPCはNPC側から読み込む
    /// データがない場合は、_masterAnimals/_subAnimalsからtypeを取得し、getCharacterPositionから位置を取得
    /// </summary>
    private (List<Vector3> positions, List<Param_AnimalInfo.AnimalType> types) LoadCharacterData(PlayerType playerType)
    {
        bool isMaster = playerType == PlayerType.Master;
        bool isNPC = playerType == PlayerType.NPC;
        
        List<Vector3> characterPositions = null;
        List<Param_AnimalInfo.AnimalType> characterTypes = null;
        
        // MasterとSubはPlayer側から、NPCはNPC側から読み込む
        if (isNPC)
        {
            // NPC側のデータを読み込む
            characterPositions = ES3.Load<List<Vector3>>(DataKey.DATAKEY_GAME_INFO + DataKey.LIST_VECTOR3_CHARACTER_POSITION_NPC);
            characterTypes = ES3.Load<List<Param_AnimalInfo.AnimalType>>(DataKey.DATAKEY_GAME_INFO + DataKey.ARRAY_INT_TEAM_FORMATION_NPC);
            Debug.Log($"[PhotonAvatarCreator] NPC側のチーム編成を読み込みました。characterTypes: [{string.Join(", ", characterTypes)}]");
            Debug.Log($"[PhotonAvatarCreator] NPC側のキャラクタ配置リストを読み込みました。characterPositions: [{string.Join(", ", characterPositions)}]");
        }
        else
        {
            // MasterとSubはPlayer側のデータを読み込む
            characterPositions = ES3.Load<List<Vector3>>(DataKey.DATAKEY_GAME_INFO + DataKey.LIST_VECTOR3_CHARACTER_POSITION_PLAYER);
            characterTypes = ES3.Load<List<Param_AnimalInfo.AnimalType>>(DataKey.DATAKEY_GAME_INFO + DataKey.ARRAY_INT_TEAM_FORMATION_PLAYER);
        }
        
        // 位置だけがない場合
        bool positionsMissing = characterPositions == null || characterPositions.Count == 0;
        // キャラクタータイプだけがない場合
        bool typesMissing = characterTypes == null || characterTypes.Count == 0;
        
        var animals = isMaster ? _masterAnimals : _subAnimals;
        
        if (positionsMissing)
        {
            characterPositions = new List<Vector3>();
            for (int i = 0; i < 4; i++)
            {
                Vector3 position = getCharacterPosition(i, isMaster);
                characterPositions.Add(position);
            }
            Debug.Log($"[PhotonAvatarCreator] キャラクタ配置リストを取得しました。PlayerType: {playerType}, characterPositions: [{string.Join(", ", characterPositions)}]");
        }
        if (typesMissing)
        {
            Debug.Log($"[PhotonAvatarCreator] キャラクタータイプがない場合は_charaListから取得 PlayerType: {playerType}");
            if (animals != null && animals.Count > 0)
            {
                characterTypes = new List<Param_AnimalInfo.AnimalType>();
                
                for (int i = 0; i < 4; i++)
                {
                    // 4体目はkeeper枠として固定
                    Param_AnimalInfo.AnimalType animalType = (i == 3)
                        ? Param_AnimalInfo.AnimalType.Bear
                        : GetAnimalTypeFromGameObject(animals[i].gameObject);
                    characterTypes.Add(animalType);
                }
                Debug.Log($"[PhotonAvatarCreator] キャラクタータイプを取得しました。PlayerType: {playerType}, characterTypes: [{string.Join(", ", characterTypes)}]");
            }
        }

        
        return (characterPositions, characterTypes);
    }
    
    /// <summary>
    /// ゲームオブジェクトからAnimalTypeを取得
    /// </summary>
    private Param_AnimalInfo.AnimalType GetAnimalTypeFromGameObject(GameObject animalObj)
    {
        if (animalObj == null)
        {
            return Param_AnimalInfo.AnimalType.None;
        }
        
        string objName = animalObj.name;
        // ゲームオブジェクト名から拡張子やCloneなどを除去
        objName = objName.Replace("(Clone)", "").Trim();
        
        // 名前から直接推測（フォールバック）
        foreach (Param_AnimalInfo.AnimalType type in System.Enum.GetValues(typeof(Param_AnimalInfo.AnimalType)))
        {
            if (objName.Contains(type.ToString()))
            {
                return type;
            }
        }
        
        return Param_AnimalInfo.AnimalType.None;
    }

    // キャラクターの位置を取得
    private Vector3 getCharacterPosition(int number, bool isMaster)
    {
        float x = 0.0f;
        float z = 0.0f;

        switch(number)
        {
            case 0:
                z = -3.0f;
                break;
            case 1:
                z = -8.5f;
                x = 4.0f;
                break;
            case 2:
                z = -8.5f;
                x = -4.0f;
                break;
            case 3:
                z = -16.5f;
                break;
        }

        z = (isMaster) ? z : -z;

        return new Vector3(x, 0.0f, z);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        // シーンロード完了をプロパティで通知
        PhotonNetwork.LocalPlayer.setIsSceneLoaded(true);
    }

    // 位置データあり版（LoadCharacterDataでデフォルトデータも設定済み）
    private GameObject SpawnCharactersWithPositions(PlayerType playerType, List<Vector3> characterPositions, List<Param_AnimalInfo.AnimalType> characterTypes, int uniformKind)    
    {
        bool isMaster = playerType == PlayerType.Master;
        bool isNPC = playerType == PlayerType.NPC;
        
        // ボール所持者インデックスを読み込む（PLAYER側は0~3、NPC側は4~7）
        int ballOwnerIndex = ES3.Load<int>(DataKey.DATAKEY_GAME_INFO + DataKey.INT_BALL_OWNER, -1);
   
        // チームに応じてインデックスを変換
        if (ballOwnerIndex >= 0)
        {
            if (isNPC)
            {
                // NPC側: 4~7を0~3に変換
                if (ballOwnerIndex >= 4 && ballOwnerIndex <= 7)
                {
                    ballOwnerIndex = ballOwnerIndex - 4;
                }
                else
                {
                    // NPC側ではない場合は-1に設定
                    ballOwnerIndex = -1;
                }
            }
            else if (isMaster)
            {
                // PLAYER側（Master）: 0~3のまま
                if (ballOwnerIndex >= 4)
                {
                    // NPC側のインデックスの場合は-1に設定
                    ballOwnerIndex = -1;
                }
            }
            else
            {
                // Sub（対人戦の相手側）の場合は-1に設定
                ballOwnerIndex = -1;
            }
        }

        GameObject ownerAnimal = null;
        
        // 4つ生成
        for(int i=0; i<4; i++)
        {
            if (characterTypes == null)
            {
                continue;
            }
            
            if (i >= characterTypes.Count)
            {
                continue;
            }
            var animalType = characterTypes[i];
            
            // characterTypesがBallの場合はスキップ（ボール用）
            if (animalType == Param_AnimalInfo.AnimalType.Ball)
            {
                continue;
            }
            
            if (animalType == Param_AnimalInfo.AnimalType.None)
            {
                continue;
            }
            
            string animalName = animalType.ToString();
            
            // 位置を取得（データがない場合はgetCharacterPositionを使用）
            Vector3 position;
            if (characterPositions != null && i < characterPositions.Count)
            {
                position = characterPositions[i];
            }
            else
            {
                position = getCharacterPosition(i, isMaster);
            }

            // 向いている方向を設定
            Quaternion rotation = isMaster ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
            
            // 生成
            GameObject animal = PhotonNetwork.Instantiate(animalName, position, rotation, 0);

            // 所持者インデックスと一致したら返す
            if (i == ballOwnerIndex)
            {
                ownerAnimal = animal;
            }

            // 名前を設定
            string nameSuffix = (playerType == PlayerType.Master) ? " (Master)" : 
                               (playerType == PlayerType.NPC) ? " (NPC)" : " (Sub)";
            animal.name = animal.name + nameSuffix;

            AnimalFacade animalFacade = animal.GetComponent<AnimalFacade>();
            PhotonAvatarContainerChild containerChild = animalFacade != null ? animalFacade.GetAvatar() : null;

            // 編成スロット（0〜3）を記録（操作割当で使用）
            var formationSlot = animal.GetComponent<AnimalFormationSlot>();
            if (formationSlot == null)
            {
                formationSlot = animal.AddComponent<AnimalFormationSlot>();
            }
            formationSlot.Initialize(i);

            if (containerChild != null && containerChild.IsMine)
            {
                // NPCならNPCタグ、PlayerならPLAYERタグをつける
                string tag = (playerType == PlayerType.NPC) ? ConstData.NPC_TAG : ConstData.PLAYER_TAG;

                // Avatar 経由でタグを設定し、Facade でユニフォームを設定
                containerChild.SetTag(tag);
                animalFacade.SetUniformType(uniformKind);
            }
        }
        
        return ownerAnimal;
    }

}
