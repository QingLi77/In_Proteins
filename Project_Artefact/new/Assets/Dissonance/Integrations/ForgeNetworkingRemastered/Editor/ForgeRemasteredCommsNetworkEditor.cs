using System.Collections.Generic;
using System.Linq;
using Dissonance.Editor;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Integrations.ForgeNetworkingRemastered.Editor
{
    [CustomEditor(typeof(ForgeRemasteredCommsNetwork))]
    public class ForgeCommsNetworkEditor
        : BaseDissonnanceCommsNetworkEditor<ForgeRemasteredCommsNetwork, ForgeRemasteredServer, ForgeRemasteredClient, ForgeRemasteredPeer, Unit, Unit>
    {
        private SerializedProperty _voiceDataChannelToServerProperty;
        private int _voiceDataChannelToServer;

        private SerializedProperty _systemMessagesChannelToServerProperty;
        private int _systemMessagesChannelToServer;

        private SerializedProperty _voiceDataChannelToClientProperty;
        private int _voiceDataChannelToClient;

        private SerializedProperty _systemMessagesChannelToClientProperty;
        private int _systemMessagesChannelToClient;

        private bool _advancedFoldout;

        protected void OnEnable()
        {
            _voiceDataChannelToServerProperty = serializedObject.FindProperty("_voiceDataChannelToServer");
            _voiceDataChannelToServer = ValueOrDefault(_voiceDataChannelToServerProperty, 57729876);

            _systemMessagesChannelToServerProperty = serializedObject.FindProperty("_systemMessagesChannelToServer");
            _systemMessagesChannelToServer = ValueOrDefault(_systemMessagesChannelToServerProperty, 57729877);

            _voiceDataChannelToClientProperty = serializedObject.FindProperty("_voiceDataChannelToClient");
            _voiceDataChannelToClient = ValueOrDefault(_voiceDataChannelToClientProperty, 57729878);

            _systemMessagesChannelToClientProperty = serializedObject.FindProperty("_systemMessagesChannelToClient");
            _systemMessagesChannelToClient = ValueOrDefault(_systemMessagesChannelToClientProperty, 57729879);
        }

        private static int ValueOrDefault([NotNull] SerializedProperty prop, int defaultValue)
        {
            var val = prop.intValue;
            if (val == 0)
                val = defaultValue;
            return val;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                serializedObject.Update();

                _advancedFoldout = EditorGUILayout.Foldout(_advancedFoldout, "Advanced Config");
                if (_advancedFoldout)
                {
                    EditorGUILayout.HelpBox("Dissonance requires 4 Forge Networking group IDs. If you are not sending custom network packets use the defaults", MessageType.Info);

                    _voiceDataChannelToServer = EditorGUILayout.DelayedIntField("Event 1", _voiceDataChannelToServer);
                    _systemMessagesChannelToServer = EditorGUILayout.DelayedIntField("Event 2", _systemMessagesChannelToServer);
                    _voiceDataChannelToClient = EditorGUILayout.DelayedIntField("Event 3", _voiceDataChannelToClient);
                    _systemMessagesChannelToClient = EditorGUILayout.DelayedIntField("Event 4", _systemMessagesChannelToClient);

                    var set = new HashSet<int>() {
                        _voiceDataChannelToServer,
                        _systemMessagesChannelToServer,
                        _voiceDataChannelToClient,
                        _systemMessagesChannelToClient
                    };

                    if (set.Count != 4)
                        EditorGUILayout.HelpBox("IDs must be unique", MessageType.Error);
                    else if (set.Any(a => a < 10000))
                        EditorGUILayout.HelpBox("IDs must be >= 10000", MessageType.Error);
                    else
                    {
                        _voiceDataChannelToServerProperty.intValue = _voiceDataChannelToServer;
                        _systemMessagesChannelToServerProperty.intValue = _systemMessagesChannelToServer;
                        _voiceDataChannelToClientProperty.intValue = _voiceDataChannelToClient;
                        _systemMessagesChannelToClientProperty.intValue = _systemMessagesChannelToClient;
                    }
                }

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
