using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;

public class FirebaseCheck : MonoBehaviour {
    DatabaseReference root;

    public void OnInitializeFirebaseDone() {
        Debug.Log("OnInitializeFirebaseDone");
        root = FirebaseDatabase.DefaultInstance.RootReference;

        root.Child("foo").GetValueAsync().ContinueWith(
            task => {
                if (task.IsFaulted) { Debug.Log("Error"); }
                else if (task.IsCompleted) {
                    Debug.Log("OK");
                    var d = (string)task.Result.Value;
                    Debug.Log(d);
                }
            });
    }
}

