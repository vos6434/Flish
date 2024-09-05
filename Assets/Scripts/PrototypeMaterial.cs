using UnityEngine;

[ExecuteInEditMode]
public class PrototypeMaterial : MonoBehaviour
{

    public float scaleFactor = 5.0f;
    Material mat;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetComponent<Renderer>().material.mainTextureScale = new Vector2(transform.localScale.x / scaleFactor, transform.localScale.z / scaleFactor);
    }

    // Update is called once per frame
    void Update()
    {

        if (transform.hasChanged && Application.isEditor && !Application.isPlaying)
        {
            Debug.Log("The transform has changed!");
            GetComponent<Renderer>().material.mainTextureScale = new Vector2(transform.localScale.x / scaleFactor, transform.localScale.z / scaleFactor);
            transform.hasChanged = false;
        }

    }
}
