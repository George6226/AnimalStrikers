using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// サメのスペシャル泡エリアの当たり判定。Body の敵陣営が触れている間 <see cref="AnimalHandler"/> のスローダウンを付与する（ダメージなし）。
/// オブジェクト破棄・非アクティブ時は <see cref="OnTriggerExit"/> が来ないことがあるため <see cref="OnDisable"/> で付与分を戻す。
/// </summary>
public class FieldCollider_SpecialBubble : MonoBehaviour
{
    private AnimalFacade _ownerFacade;

    /// <summary>この泡が <see cref="AnimalHandler.AddSharkBubbleSlowdownSource"/> を呼んだ回数（Exit / Disable で戻す）。</summary>
    private readonly Dictionary<AnimalHandler, int> _slowAddsByHandler = new Dictionary<AnimalHandler, int>();

    private void OnDisable()
    {
        ReleaseAllSlowdownFromThisBubble();
    }

    private void ReleaseAllSlowdownFromThisBubble()
    {
        if (_slowAddsByHandler.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<AnimalHandler, int> kv in new List<KeyValuePair<AnimalHandler, int>>(_slowAddsByHandler))
        {
            AnimalHandler h = kv.Key;
            if (h == null)
            {
                continue;
            }

            for (int i = 0; i < kv.Value; i++)
            {
                h.RemoveSharkBubbleSlowdownSource();
            }
        }

        _slowAddsByHandler.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!TryGetOpponentFromBodyCollider(other, out AnimalFacade target))
        {
            return;
        }

        AnimalHandler handler = target.GetAnimalHandler();
        if (handler == null)
        {
            return;
        }

        if (!_slowAddsByHandler.ContainsKey(handler))
        {
            _slowAddsByHandler[handler] = 0;
        }

        _slowAddsByHandler[handler]++;
        handler.AddSharkBubbleSlowdownSource();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!TryGetOpponentFromBodyCollider(other, out AnimalFacade target))
        {
            return;
        }

        AnimalHandler handler = target.GetAnimalHandler();
        if (handler == null)
        {
            return;
        }

        if (_slowAddsByHandler.TryGetValue(handler, out int n) && n > 0)
        {
            int left = n - 1;
            if (left <= 0)
            {
                _slowAddsByHandler.Remove(handler);
            }
            else
            {
                _slowAddsByHandler[handler] = left;
            }

            handler.RemoveSharkBubbleSlowdownSource();
        }
    }

    private bool TryGetOpponentFromBodyCollider(Collider other, out AnimalFacade target)
    {
        target = null;
        if (other == null)
        {
            return false;
        }

        if (!other.gameObject.tag.Equals("Body"))
        {
            return false;
        }

        if (_ownerFacade == null)
        {
            return false;
        }

        var ownerAvatar = _ownerFacade.GetAvatar();
        if (ownerAvatar == null)
        {
            return false;
        }

        AnimalFacade t = other.transform.parent != null
            ? other.transform.parent.GetComponent<AnimalFacade>()
            : null;
        if (t == null || t == _ownerFacade)
        {
            return false;
        }

        var targetAvatar = t.GetAvatar();
        if (targetAvatar == null)
        {
            return false;
        }

        if (IsSameFaction(ownerAvatar, targetAvatar))
        {
            return false;
        }

        target = t;
        return true;
    }

    private static bool IsSameFaction(PhotonAvatarContainerChild a, PhotonAvatarContainerChild b)
    {
        return string.Equals(TagForCompare(a), TagForCompare(b), System.StringComparison.Ordinal);
    }

    private static string TagForCompare(PhotonAvatarContainerChild avatar)
    {
        string tag = avatar.CurrentTag;
        if (string.IsNullOrEmpty(tag))
        {
            tag = avatar.gameObject.tag;
        }

        return tag;
    }

    public void SetOwnerFacade(AnimalFacade animalFacade)
    {
        _ownerFacade = animalFacade;
    }
}
