using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    static readonly Queue<Action> _actions = new Queue<Action>();
    static readonly object _lock = new object();

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public static void Enqueue(Action action)
    {
        if (action == null) return;
        lock (_lock) { _actions.Enqueue(action); }
    }

    void Update()
    {
        while (true)
        {
            Action action = null;
            lock (_lock)
            {
                if (_actions.Count == 0) break;
                action = _actions.Dequeue();
            }
            try { action?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
