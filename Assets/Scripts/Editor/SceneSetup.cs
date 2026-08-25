#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class SceneSetup
{
    [MenuItem("Tools/Setup Game Scene")]
    public static void BuildScene()
    {
        Scene scene = SceneManager.GetActiveScene();

        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
        }
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(8f, 1f, 8f);

        Material groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        groundMaterial.name = "GroundMaterial";
        groundMaterial.SetColor("_BaseColor", new Color(0.32f, 0.53f, 0.25f));
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
        }
        player.transform.position = new Vector3(0f, 1f, 0f);

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null) rb = player.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if (player.GetComponent<PlayerController>() == null) player.AddComponent<PlayerController>();

        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cam = cameraObject.AddComponent<Camera>();
        }
        if (cam.GetComponent<CameraFollow>() == null) cam.gameObject.AddComponent<CameraFollow>();
        CameraFollow follow = cam.GetComponent<CameraFollow>();
        follow.target = player.transform;

        if (GameObject.Find("Directional Light") == null)
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Scene ready: ground, player, follow camera and light are set up.");
    }
}
#endif
