using UnityEngine;
using System.Collections;

public class ExActivateOnLevelState : MonoBehaviour {

    public ExLevelController.LevelState ActiveState;

    void Awake()
    {
    }
    void Start()
    {
        if(FFSystem.OwnGameObject(gameObject))
        {
            FFMessage<ChangeLevelStateEvent>.Connect(OnChangeLevelStateEvent);
        }
    }

    void OnDestroy()
    {
        FFMessage<ChangeLevelStateEvent>.Disconnect(OnChangeLevelStateEvent);
    }

    private void OnChangeLevelStateEvent(ChangeLevelStateEvent e)
    {
        if(e.newState == ActiveState)
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
