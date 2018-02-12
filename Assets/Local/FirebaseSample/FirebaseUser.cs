using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;

public class FirebaseUser : MonoBehaviour {
    DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;
    DatabaseReference root;

    [SerializeField] string editorDatabaseHostName;
    [SerializeField] UnityEvent onInitializeDone;

    Action a;

    void Start() {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
                dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available) {
                    InitializeFirebase();
                } else {
                    Debug.LogError(
                        "Could not resolve all Firebase dependencies: " +
                        dependencyStatus);
                }
            });
    }

    void Update() {
        if (a != null) {
            a();
            a = null;
        }
    }

    

    // Initialize the Firebase database:
    void InitializeFirebase() {
        FirebaseApp app = FirebaseApp.DefaultInstance;
        app.SetEditorDatabaseUrl("https://" + editorDatabaseHostName + ".firebaseio.com/");
        if (app.Options.DatabaseUrl != null) {
            app.SetEditorDatabaseUrl(app.Options.DatabaseUrl);
        }
        root = FirebaseDatabase.DefaultInstance.RootReference;
        Debug.Log("InitializeFirebase");
        a = onInitializeDone.Invoke;
    }

}