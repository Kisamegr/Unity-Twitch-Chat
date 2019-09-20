using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Box : MonoBehaviour
{
    private GameObject canvas;

    public void Initialize(string boxName, Color boxColor, float boxSize)
    {
        transform.localScale *= boxSize;

        var spriteRenderer = GetComponent<SpriteRenderer>();
        var text = GetComponentInChildren<Text>().text = boxName;
        spriteRenderer.color = boxColor;

        GetComponent<Rigidbody2D>().AddForce(new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 7.5f, ForceMode2D.Impulse);
        GetComponent<Rigidbody2D>().AddTorque(Random.Range(-1f, 1f) * 50f);

        canvas = transform.GetChild(0).gameObject;
        //Detach name canvas from parent so it doesn't rotate with the box
        canvas.transform.SetParent(null);

        StartCoroutine(JumpLoop());
    }

    private void Update()
    {
        //Move name canvas above the box
        canvas.transform.position = transform.position + new Vector3(0, 2, 0);
    }

    private IEnumerator JumpLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(0.5f, 2f));

            GetComponent<Rigidbody2D>().AddForce(new Vector2(Random.Range(-1f, 1f), 1f) * 7.5f, ForceMode2D.Impulse);
            GetComponent<Rigidbody2D>().AddTorque(Random.Range(-1f, 1f) * 50f);
        }
    }
}
