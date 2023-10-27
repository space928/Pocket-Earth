using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public class Planet : MonoBehaviour
    {
        [SerializeField] private float radius;
        [SerializeField] private Texture2D terrainMap;


        public float Radius => radius;

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}