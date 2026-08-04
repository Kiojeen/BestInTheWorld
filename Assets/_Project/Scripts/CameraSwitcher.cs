using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private CinemachineCamera introCamera;
    [SerializeField] private CinemachineCamera gameplayCamera;

    private PlayableDirector director;

    void Awake()
    {
        director = GetComponent<PlayableDirector>();
        director.stopped += OnTimelineFinished;
    }

    void OnTimelineFinished(PlayableDirector director)
    {
        introCamera.Priority = 10;
        gameplayCamera.Priority = 20;
    }
}