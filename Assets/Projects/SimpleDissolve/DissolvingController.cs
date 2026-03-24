using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class DissolvingController : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMesh;
    public VisualEffect VFXGraph;
    public VisualEffect VFXGraph_2;
    public float dissolveRate = 0.0125f;
    public float refreshRate = 0.025f;
    private Material[] skinnedMaterials;

    public void Start()
    {
        if (skinnedMesh) {
            skinnedMaterials = skinnedMesh.materials;
        }
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space)) {
            StartCoroutine(DissolveCo());
        }
    }

    IEnumerator DissolveCo() {
        if (VFXGraph != null) {
            VFXGraph.Play();
        }
        if (VFXGraph_2 != null) {
            VFXGraph_2.Play();
        }

        if (skinnedMaterials.Length > 0) {
            float counter = 0;
            while (skinnedMaterials[0].GetFloat("_DissolveAmount") < 1)
            {
                counter += dissolveRate;
                for (int i=0; i < skinnedMaterials.Length; i++) {
                    skinnedMaterials[i].SetFloat("_DissolveAmount", counter);
                }

                yield return new WaitForSeconds(refreshRate);
            }
        }
    }
}
