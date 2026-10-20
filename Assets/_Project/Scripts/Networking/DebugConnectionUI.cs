using Unity.Netcode;
using UnityEngine;

namespace BeyondLimits.Networking
{
    /// <summary>
    /// Temporary debug UI to start a host, client or server.
    /// Will be replaced by the session menu in Phase 2.
    /// </summary>
    public class DebugConnectionUI : MonoBehaviour
    {
        private void OnGUI()
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null) return;

            // Hide the buttons once a connection is running.
            if (manager.IsClient || manager.IsServer) return;

            GUILayout.BeginArea(new Rect(20, 20, 240, 160));
            if (GUILayout.Button("Host")) manager.StartHost();
            if (GUILayout.Button("Client")) manager.StartClient();
            if (GUILayout.Button("Server")) manager.StartServer();
            GUILayout.EndArea();
        }
    }
}
