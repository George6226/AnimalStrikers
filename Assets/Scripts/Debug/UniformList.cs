using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UniformList : MonoBehaviour
{
    [SerializeField] Renderer _uniformRenderer;
    [SerializeField] List<Texture> _blueTex;
    [SerializeField] List<Texture> _redTex;
    [SerializeField] Renderer _shoesRenderer;
    [SerializeField] List<Texture> _shoesTex;
    bool _blueTeam = true;

    public void SetTeamColor(bool isBlue)
    {
        _blueTeam = isBlue;
    }

    public void ChangeUniform(int texNum)
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        //���j�t�H�[���e�N�X�`���ύX
        _uniformRenderer.GetPropertyBlock(mpb);
        Texture targetTex = _blueTeam ? _blueTex[texNum] : _redTex[texNum];
        mpb.SetTexture("_BaseMap", targetTex);
        _uniformRenderer.SetPropertyBlock(mpb);
        //�V���[�Y�e�N�X�`���ύX
        _shoesRenderer.GetPropertyBlock(mpb);
        targetTex = _blueTeam ? _shoesTex[0] : _shoesTex[1];
        mpb.SetTexture("_BaseMap", targetTex);
        _shoesRenderer.SetPropertyBlock(mpb);
    }

    public int getUniformNum(bool isBlue)
    {
        return isBlue ? _blueTex.Count : _redTex.Count;
    }
}
