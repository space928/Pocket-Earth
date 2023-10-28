using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    [ExecuteInEditMode]
    public class Atmosphere : MonoBehaviour
    {
        [SerializeField] private Light sun;
        [SerializeField] private Material atmosphereMat;

        // Update is called once per frame
        void Update()
        {
            atmosphereMat.SetVector("_LightDir", sun.transform.forward);
        }
    }
}