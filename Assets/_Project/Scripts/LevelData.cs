using UnityEngine;

[System.Serializable]
public struct Companion
{
    public GameObject companionAvatar;
    public bool isOwned;
}

[System.Serializable]
public class LevelData
{
    public string avatarName;
    public GameObject playerAvatar;

    public Companion[] playerCompanions;
    public bool playerCanHaveCompanions;

    public int maxLevelScore;

    public GameObject[] food;
    public ParticleSystem collectEffect;

    public ParticleSystem avatarChangeEffect;
}