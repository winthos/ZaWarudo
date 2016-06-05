using UnityEngine;
using System.Collections;
using System.Reflection;
using System;

static class FFExtensionMethods
{
    // ExtensionMethods

    #region Gameobject
    public static void Destroy(this GameObject _go)
    {
        FFSystem.UnRegisterNetGameObject(_go, true);
    }
    public static bool isOwnedNetObject(this GameObject _go)
    {
        return FFSystem.OwnGameObject(_go);
    }
    public static T GetOrAddComponent<T>(this GameObject _go) where T : Component
    {
        T comp = _go.GetComponent<T>();
        if (comp != null)
            return comp;
        else
            return _go.AddComponent<T>();
    }
    #endregion


    #region MonoBehaviour
    // Add FFRef creators functions below
    public static FFRef<Vector3> ffposition(this MonoBehaviour monoBehavior)
    {
        return new FFRef<Vector3>(() => monoBehavior.transform.position,
            (v) => { monoBehavior.transform.position = v; });
    }
    public static FFRef<Vector3> ffrotation(this MonoBehaviour monoBehavior)
    {
        return new FFRef<Vector3>(() => monoBehavior.transform.eulerAngles,
            (v) => { monoBehavior.transform.rotation = Quaternion.Euler(v); });
    }
    public static FFRef<Vector3> ffscale(this MonoBehaviour monoBehavior)
    {
        return new FFRef<Vector3>(() => monoBehavior.transform.localScale, (v) => { monoBehavior.transform.localScale = v; });
    }

    
    // Componenet References
    public static FFAction action(this MonoBehaviour monoBehavior)
    {
        return monoBehavior.gameObject.GetOrAddComponent<FFAction>();
    }

    #endregion MonoBehaviour

    #region Transform
    public static T GetOrAddComponent<T>(this Transform _trans) where T : Component
    {
        T comp = _trans.GetComponent<T>();
        if (comp != null)
            return comp;
        else
            return _trans.gameObject.AddComponent<T>();
    }
    #endregion

    #region AnimationCurve
    public static float TimeToComplete(this AnimationCurve _curve)
    {
        if(_curve == null) throw new ArgumentNullException("_curve");
        else if (_curve.length < 1) { throw new Exception("_curve has less than 2 key points"); }
        else return _curve.keys[_curve.length - 1].time - _curve.keys[0].time;
    }
    #endregion

}
