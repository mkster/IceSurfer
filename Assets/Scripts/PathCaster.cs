using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PathCaster : MonoBehaviour
{

    public Transform touchTarget;
    public Transform cam;
    public GameObject prefabPathTileMesh;
    float moveStartTime;
    public float slideSpeed; //this is affected by tile length
    float aimDist = 6f; //how far forward to aim
    static float tileSpacing;
    Vector3 camOff;
    Rigidbody rb;
    LineRenderer lr;
    bool dead = false;
    float tileLength;
    float playerHeight;
    bool waitForInput = true;

    // Start is called before the first frame update
    void Start()
    {
        playerHeight = GetComponent<Collider>().bounds.extents.y;
        lr = GetComponent<LineRenderer>();
        rb = GetComponent<Rigidbody>();
        tileLength = prefabPathTileMesh.GetComponent<MeshGenRectPath>().l;
        tileSpacing = tileLength * 2f;
        slideSpeed /= tileLength;
        camOff = cam.position - transform.position;
        moveStartTime = Time.time;
        InitTouchTargetPos();

        //just to avoid errors on first path
        AddTile(Vector3.back * 10f);
        AddTile(Vector3.back * 10f);
    }

    void InitTouchTargetPos(){
        Vector3 targetPos = this.transform.position + Vector3.forward * aimDist;
        touchTarget.position = targetPos;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Break();
        }

        if (dead) return;
        UpdateTouchControls();
        UpdateAddPath(); 
        UpdateAddPath();
        UpdateAddPath();
        UpdateSliding();
        UpdateCam();

        //set line last
        lr.SetPosition(0, transform.position);
        lr.SetPosition(1, touchTarget.position);
    }

    void FixedUpdate() {
        if (dead) return;
        UpdateWalking();
    }


    Vector2 lastDragPos = new Vector2();
    bool fingerDown = false;
    void UpdateTouchControls(){
        Vector3 p = touchTarget.position;
        touchTarget.position = new Vector3(p.x, p.y, this.transform.position.z + aimDist);
        if (Input.touchCount <= 0) return;

        Touch t = Input.GetTouch(0);
        Vector2 scale = new Vector2(Screen.width, Screen.width);
        Vector2 pos = t.position / scale;
        if (t.phase == TouchPhase.Began){
            lastDragPos = pos;
            fingerDown = true;
            waitForInput = false;
            lr.enabled = true;
            touchTarget.gameObject.SetActive(true);
            InitTouchTargetPos();
        }
        else if (t.phase == TouchPhase.Moved){
            Vector2 drag = lastDragPos - pos;
            touchTarget.position -= (Vector3)drag * 20f;
            lastDragPos = pos;
        }
        else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled){
            //stop drawing drop player down
            fingerDown = false;
            lr.enabled = false;
            touchTarget.gameObject.SetActive(false);
        }
    }

    void UpdateCam(){
        Vector3 camPos = (transform.position + camOff);
        Vector3 aim = Vector3.forward + transform.position;
        float yOff = 0;
        if (fingerDown){
            aim = touchTarget.position;
            //copy vertical angle between player and aim and apply to cam smoothly
            yOff = (transform.position - aim).y * 0.5f;
        }
        Vector3 pos = camPos * 0.8f + aim * 0.2f;
        pos.z = camPos.z;
        pos += Vector3.up * yOff;
        //cam.position = cam.position*0.9f + pos*0.1f;
        cam.position = Vector3.Lerp(cam.position, pos, Time.deltaTime * 25f);

        // Smoothly rotate towards the target point.
        var targetRotation = Quaternion.LookRotation(aim - cam.position);
        cam.rotation = Quaternion.Slerp(cam.rotation, targetRotation, 2f * Time.deltaTime);
    }

    void UpdateAddPath()
    {
        if (!fingerDown) return;
        //dist between target and prev node
        //if bigger then create new tile and position
        Transform prevTile = pathTiles[(iCurrentTile - 1) % nTiles];
        float dist = Vector3.Distance(prevTile.position, touchTarget.position); 
        float zDist = touchTarget.position.z - prevTile.position.z;
        if (dist >= tileSpacing && zDist > 0f)
        {
            Vector3 pos = prevTile.position + Vector3.forward * tileSpacing;
            if (transform.position.z > prevTile.position.z + tileLength)
            {
                //cant connect to prev path, start new path
                iCurrentSlideTile = iCurrentTile; //start sliding at next
                pos = transform.position - playerHeight * Vector3.up;
                //moveStartTime = Time.time;
                Debug.Log("start new");
            }
            else
            {
                //add prev tile shift to position and add
                pos += prevTile.GetComponent<MeshGenRectPath>().off;
            }
            Transform tile = AddTile(pos);
            MeshGenRectPath mg = tile.GetComponent<MeshGenRectPath>();
            Vector3 dir = touchTarget.position - pos;
            mg.GenRect(dir);
        }
    }

    //add and rotate through array to delete old
    static int nTiles = 25;
    int iCurrentTile = 0;
    Transform[] pathTiles = new Transform[nTiles];
    Transform AddTile(Vector3 pos)
    {
        Transform tile = Instantiate(prefabPathTileMesh, pos, Quaternion.identity).transform;
        
        //delete old tile in that array place
        Transform oldTile = pathTiles[iCurrentTile % nTiles];
        if (oldTile != null) Destroy(oldTile.gameObject);
        
        pathTiles[iCurrentTile % nTiles] = tile;
        iCurrentTile++;
        return tile;
    }

    int moveTilesOffest = 4;
    int iCurrentSlideTile = 1;
    bool isSliding = false;
    void UpdateSliding(){
        Transform prevTile = pathTiles[(iCurrentSlideTile - 1) % nTiles];
        Transform tile = pathTiles[(iCurrentSlideTile) % nTiles];
        isSliding = tile != null && prevTile.position.z < tile.position.z;
        if (isSliding) {
            //slide
            float t = (Time.time - moveStartTime) * slideSpeed;
            transform.position = Vector3.Lerp(prevTile.position, tile.position, t);
            transform.position += Vector3.up * 0.75f; //offset by height of player
            if (t >= 1) {
                //finished this tile
                t = Mathf.Min(t, 2);
                moveStartTime = Time.time - (t-1)/slideSpeed;
                iCurrentSlideTile++;
                tile = pathTiles[(iCurrentSlideTile) % nTiles];
                //path is over
                if (tile == null || prevTile.position.z > tile.position.z){
                    Debug.Log("Path over"); 
                    rb.velocity = new Vector3(0, 8, 8); //jump
                }
                else{
                    UpdateSliding();
                    isSliding = true;
                }

            }
        }
        //else walk in fixed udate 
    }

    float walkSpeed = 15f;
    void UpdateWalking(){
        if (isSliding || waitForInput) return;
        if (IsGrounded())
        {
            rb.velocity = Vector3.forward * walkSpeed;
        }
        else
        {
            rb.velocity += Physics.gravity * 0.05f;
        }
    }
    
    
    // util
    bool IsGrounded()
    {
        float distToGround = GetComponent<Collider>().bounds.extents.y; //size
        return Physics.Raycast(transform.position, -Vector3.up, distToGround + 0.1f);
    }

    void OnCollisionEnter(Collision other) {
        if (dead) return;
        Vector3 normal = other.contacts[0].normal;
        //die if not landing on flat surface or sliding into anything
        if (normal != Vector3.up || isSliding) {
            dead = true;
            rb.constraints = RigidbodyConstraints.None; 
            rb.useGravity = true;
            rb.velocity = Vector3.forward * 10f;
            rb.angularVelocity = Vector3.forward * 20f;
            touchTarget.gameObject.SetActive(false);
            GetComponent<LineRenderer>().enabled = false;
            Invoke("ReloadScene", 1.5f);
        }
    }


    void ReloadScene(){
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void DeletePath(){
        foreach (Transform tile in pathTiles)
        {
            Destroy(tile.gameObject);
        }
    }

}
