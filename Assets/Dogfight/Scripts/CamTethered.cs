using UnityEngine;

public class CamTethered : MonoBehaviour
{
    [SerializeField]
    private Transform anchor;
    [SerializeField]
    private int nLinks = 10;
    [SerializeField]
    private float distance = 10f;
    [SerializeField]
    private float clearRadius = 2f;
    [SerializeField]
    private float relaxLookAt = 5f;
    [SerializeField]
    private float relaxPosition = 20f;
    [SerializeField]
    private float drag = 0f;
    [SerializeField]
    private bool flip;
    [SerializeField]
    private GameObject linkPrefab;
    [SerializeField]
    private AsteroidField asteroidField;
    private Rigidbody[] links;
    private Vector3 lookAt;
    private Vector3 lookAtLate;
    private Vector3 position;
    private Vector3 anchorPos;
    private int targetFrameRate;
    private float linkSpacing;
    private bool initialized;

    private void Start()
    {
        targetFrameRate = Application.targetFrameRate > 0 ? Application.targetFrameRate : 60;
        transform.position = anchor.position - anchor.forward * distance;
    }

    private void Initialize()
    {
        links = new Rigidbody[nLinks];
        linkSpacing = distance / (float)nLinks;
        Rigidbody rb = anchor.GetComponent<Rigidbody>();
        GameObject tether = new GameObject();
        tether.name = "Tether";
        for (int i = 0; i < nLinks; i++)
        {
            GameObject link = Instantiate(linkPrefab, rb.position - anchor.forward * linkSpacing,
                Quaternion.identity, tether.transform);
            link.GetComponent<SpringJoint>().connectedBody = rb;
            link.GetComponent<SphereCollider>().radius = i < nLinks - 1 ? linkSpacing : clearRadius;
            rb = link.GetComponent<Rigidbody>();
            links[i] = rb;
        }
        links[nLinks - 1].drag = drag;
        initialized = true;
    }

    private void ResetLinks()
    {
        anchorPos = anchor.position;
        for (int i = 0; i < nLinks; i++)
        {
            links[i].velocity = Vector3.zero;
            links[i].angularVelocity = Vector3.zero;
            links[i].position = anchorPos - anchor.forward * (i + 1) * linkSpacing;
        }
        lookAt = anchorPos;
        lookAtLate = lookAt;
        position = links[nLinks - 1].position;
        transform.position = flip ? lookAt - (position - lookAt) : position;
        transform.LookAt(lookAt);
    }

    private void FixedUpdate()
    {
        if (initialized)
        {
            position = Vector3.Lerp(position, links[nLinks - 1].position, 1 / relaxPosition);
            lookAt = Vector3.Slerp(lookAt, anchor.position, 1 / relaxLookAt);
        }
    }

    private void LateUpdate()
    {
        if (initialized)
        {
            if ((anchor.position - anchorPos).sqrMagnitude > 100)
            {
                ResetLinks();
            }
            else
            {
                float t = Time.deltaTime * targetFrameRate;
                transform.position = Vector3.Lerp(transform.position, 
                    flip ? lookAt - (position - lookAt) : position, t);
                lookAtLate = Vector3.Slerp(lookAtLate, lookAt, t);
                transform.LookAt(lookAtLate);
            }
            anchorPos = anchor.position;
            asteroidField?.UpdateBounds(lookAtLate);
        }
        else if (anchor.GetComponent<Rigidbody>().velocity.sqrMagnitude > Mathf.Epsilon)
        {
            Initialize();
        }
    }
}
