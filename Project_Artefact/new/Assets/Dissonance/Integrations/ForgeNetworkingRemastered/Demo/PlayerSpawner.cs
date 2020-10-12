using BeardedManStudios.Forge.Networking.Unity;
using UnityEngine;

namespace Dissonance.Integrations.ForgeNetworkingRemastered.Demo
{
    public class PlayerSpawner
        : MonoBehaviour
    {
        private static readonly System.Random Rand = new System.Random();

        private void Start()
        {
            var manager = NetworkManager.Instance;

            //We don't know if the user has defined the DissonanceDemoPlayer contracts yet, reflect for the instantiate method and call it if it exists
            var method = manager.GetType().GetMethod("InstantiateDissonanceDemoPlayer");
            if (method != null)
            {
                method.Invoke(
                    manager,
                    new object[] {
                        Random.Range(0, 4),
                        //new Vector3(Rand.Next(-15, 15), 0, Rand.Next(-15, 15)),
                        new Vector3(Rand.Next(-15, 15), 0, -5),
                        null,


                        true
                    });

                Destroy(gameObject);
            }
            else
            {
                //Can't find the method, show the warning text
                GetComponentInChildren<Canvas>().enabled = true;
            }
        }
    }
}
