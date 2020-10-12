// /* Comment out this line to uncomment the entire file

using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Generated;
using UnityEngine;

namespace Dissonance.Integrations.ForgeNetworkingRemastered.Demo
{
    public class ForgeRemasteredDissonanceDemoPlayer
        : DissonanceDemoPlayerBehavior, IDissonancePlayer
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Network, "FNR Player Component");

        private DissonanceComms _comms;

        public bool IsTracking { get; private set; }

        public string PlayerId { get; private set; }

        public Vector3 Position
        {
            get { return transform.position; }
        }

        public Quaternion Rotation
        {
            get { return transform.rotation; }
        }

        public NetworkPlayerType Type
        {
            get
            {
                if (PlayerId == null)
                    return NetworkPlayerType.Unknown;

                return networkObject.IsOwner
                     ? NetworkPlayerType.Local
                     : NetworkPlayerType.Remote;
            }
        }
        #endregion

        public void OnEnable()
        {
            //We can't do anything useful if we can't find a DissonanceComms component to interact with!
            _comms = FindObjectOfType<DissonanceComms>();
            if (_comms == null)
            {
                throw Log.CreateUserErrorException(
                    "cannot find DissonanceComms component in scene",
                    "not placing a DissonanceComms component on a game object in the scene",
                    "https://dissonance.readthedocs.io/en/latest/Basics/Quick-Start-Forge-Remastered/",
                    "60359FAC-3CDD-4971-B7C6-585283BB023B");
            }
        }

        public void OnDisable()
        {
            //Unregistered this player object from Dissonance
            if (IsTracking)
                StopTracking();
        }

        public void OnDestroy()
        {
            //Make sure to clean upe vent listeners when this entity is destroyed
            if (_comms != null)
                _comms.LocalPlayerNameChanged -= SetPlayerId;
        }

        private void StartTracking()
        {
            //Sanity check!
            if (IsTracking)
                throw Log.CreatePossibleBugException("Attempting to start player tracking, but tracking is already started", "993F96B0-127B-445E-82FE-CE86534CDAF0");

            //Register this player object with Dissonance, Dissonance will then use this tracker to drive positional audio playback
            if (_comms != null)
            {
                _comms.TrackPlayerPosition(this);
                IsTracking = true;
            }
        }

        private void StopTracking()
        {
            //Sanity check!
            if (!IsTracking)
                throw Log.CreatePossibleBugException("Attempting to stop player tracking, but tracking is not started", "A236752E-2CE4-4EA8-B370-1B798C812622");

            //Stop tracking in Dissonance (positional audio playback will not work for this player while not tracking)
            if (_comms != null)
            {
                _comms.StopTracking(this);
                IsTracking = false;
            }
        }

        protected override void CompleteRegistration()
        {
            base.CompleteRegistration();

            //This is a forge event indicating that this object is completely setup in the network session. Now we can do our initial setup.
            //If this is a local player object then set the ID to whatever the local Dissonance ID is (and make sure to register for ID change events)
            if (networkObject.IsOwner)
            {
                Log.Trace("Initializing local FNR player");

                //If the name is initialized in Dissonance set it into the tracker
                if (_comms.LocalPlayerName != null)
                    SetPlayerId(_comms.LocalPlayerName);

                //Watch for future name changes
                _comms.LocalPlayerNameChanged += SetPlayerId;
            }
        }

        private void SetPlayerId(string id)
        {
            //We cannot change the name while tracking is running, so stop it if necessary before changing the name.
            if (IsTracking)
                StopTracking();

            //Perform the name change and start tracking back up
            PlayerId = id;
            StartTracking();

            //If we're the owner then proprogate this change to other peers
            if (networkObject.IsOwner)
                networkObject.SendRpc(RPC_SET_DISSONANCE_ID, Receivers.OthersBuffered, id);
        }

        public override void SetDissonanceId(RpcArgs args)
        {
            //Received an RPC indicating the ID has been set elsewhere. Update the local ID.
            SetPlayerId(args.GetNext<string>());
        }

        private void Update()
        {
            if (!networkObject.IsOwner)
            {
                //Read out the transform data from the network and sync this object (since it is not locally controlled)
                transform.position = networkObject.position;
                transform.rotation = networkObject.rotation;
            }
            else
            {
                //Get player input from the input axes
                var rotation = Input.GetAxis("Horizontal") * Time.deltaTime * 150.0f;
                var speed = Input.GetAxis("Vertical") * 3.0f;

                //Turn
                //transform.Rotate(0, rotation, 0);

                //Move
                var forward = transform.TransformDirection(Vector3.forward);
                var controller = GetComponent<CharacterController>();
                controller.SimpleMove(forward * speed);

                //Reset position if we've fallen off the world
                if (transform.position.y < -3)
                {
                    transform.position = Vector3.zero;
                    transform.rotation = Quaternion.identity;
                }

                //Update the network object with transform data. Forge will sync this across the network
                networkObject.position = transform.position;
                networkObject.rotation = transform.rotation;
            }
        }
    }
}

// */