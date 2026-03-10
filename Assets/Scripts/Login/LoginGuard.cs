using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a root GameObject in any scene that requires login.
/// If the user is not logged in, redirects back to the Login-Scene.
/// </summary>
public class LoginGuard : MonoBehaviour
{
    [Tooltip("Scene to redirect to if the user is not logged in.")]
    public string loginSceneName = "Login-Scene";

    private void Awake()
    {
        // No UserSession or not logged in → force back to login
        if (UserSession.Instance == null || !UserSession.Instance.IsLoggedIn)
        {
            Debug.LogWarning("[LoginGuard] User not authenticated. Redirecting to login.");
            SceneManager.LoadScene(loginSceneName);
        }
    }
}
