using UnityEngine;
using System.Collections;

public class ExDestroyOnLevelState : FFComponent {

    public ExLevelController.LevelState DestroyState;

    void Awake()
    {
    }

    void Start()
    {
        FFMessage<ChangeLevelStateEvent>.Connect(OnChangeLevelStateEvent);
    }
    void OnDestroy()
    {
        FFMessage<ChangeLevelStateEvent>.Disconnect(OnChangeLevelStateEvent);
    }


    private void OnChangeLevelStateEvent(ChangeLevelStateEvent e)
    {
        if (e.newState == DestroyState)
        {
            gameObject.Destroy();
        }
    }
}
