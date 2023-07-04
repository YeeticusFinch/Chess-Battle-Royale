using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Player : NetworkBehaviour {

    public List<Piece> flunkies;
    //public bool isLocalPlayer = false;
    //public bool isServer = false;
    public int[] loc = { 0, 0 };
    public int playerNum = -1;
    public int points = 0;
    public bool spawned = false;
    public bool cpu = false;

    public SpawnSet[] spawnSets;

    [System.Serializable]
    public struct SpawnSet {
        public SpawnSet(GameObject prefab, int n)
        {
            this.prefab = prefab;
            this.n = n;
        }
        public GameObject prefab;
        public int n;
    }

    // Use this for initialization
    void Start () {
        transform.eulerAngles = new Vector3(-90, 0, 180);
        int[] newSpawn = new int[] { (int)Random.Range(-1000, 1000), (int)Random.Range(-1000, 1000) };
        if (!isServer)
        {
            CmdRespawn(newSpawn);
            Respawn(newSpawn);
        }
        else
        {
            playerNum = 0;
            RpcRespawn(newSpawn);
        }
        Color color = Game.playerColors[playerNum];
        Color color2 = new Color(color.b, color.r, color.g);
        GetComponent<Renderer>().materials[0].SetColor("_Color", color);
        GetComponent<Renderer>().materials[0].EnableKeyword("_EMISSION");
        GetComponent<Renderer>().materials[0].SetColor("_EmissionColor", color);

        GetComponent<Renderer>().materials[1].SetColor("_Color", color2);
        GetComponent<Renderer>().materials[1].EnableKeyword("_EMISSION");
        GetComponent<Renderer>().materials[1].SetColor("_EmissionColor", color2);
        GetComponent<Renderer>().materials[1].SetFloat("_EmissionStrength", 3);
    }
	
	// Update is called once per frame
	void Update () {
		if (isLocalPlayer)
        {
            CheckControls();
        }
	}

    //int prevMoveX = 0;
    //int prevMoveY = 0;
    float prevMoveTime = 0;

    public Camera cam;
    public Piece selPiece = null;
    public GameObject availableMove;
    GameObject availableMoveInstance;

    void CheckControls()
    {
        if (!spawned) return;
        
        if (Input.GetMouseButton(0))
        {
            //Debug.Log("Clicking");
            RaycastHit hit;
            if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit))
            {
                int[] hitLoc = { Mathf.RoundToInt((hit.point.x - 1) / 2), Mathf.RoundToInt((hit.point.z - 1) / 2) };

                if (availableMoveInstance == null) {
                    availableMoveInstance = GameObject.Instantiate(availableMove);
                }
                availableMoveInstance.transform.position = new Vector3(hitLoc[0]*2+1, -6.9f, hitLoc[1]*2+1);
                //availableMoveInstance.transform.position = hit.point;

                //Debug.Log("Clicked board at " + hitLoc[0] + ", " + hitLoc[1]);
                //Debug.Log("SelPiece = " + selPiece);

                if (selPiece == null && Game.pieces.ContainsKey(hitLoc))
                    selPiece = Game.pieces[hitLoc];
          

                else if (selPiece != null && selPiece.canMove)
                {
                    int[] targetLoc = selPiece.GetClosestMove(hitLoc);

                    if (!isServer)
                    {
                        CmdMovePiece(selPiece.loc, targetLoc, 0.1f);
                        MovePiece(selPiece.loc, targetLoc, 0.1f);
                    }
                    else
                    {
                        RpcMovePiece(selPiece.loc, targetLoc, 0.1f);
                    }
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            selPiece = null;
            if (availableMoveInstance != null)
                Destroy(availableMoveInstance);
        }

        int moveX = Input.GetAxisRaw("Horizontal") == 0 ? 0 : (int)Mathf.Sign(Input.GetAxisRaw("Horizontal"));
        int moveY = Input.GetAxisRaw("Vertical") == 0 ? 0 : (int)Mathf.Sign(Input.GetAxisRaw("Vertical"));

        if (Time.fixedTime-prevMoveTime > 0.1f && (moveX != 0 || moveY != 0))
        {
            prevMoveTime = Time.fixedTime;
            //prevMoveX = moveX;
            //prevMoveY = moveY;
            float seed = Random.Range(0f, 1f);
            if (!isServer)
            {
                CmdMovePlayer(new int[] { loc[0] + moveX, loc[1] + moveY }, seed);
                MovePlayer(new int[] { loc[0] + moveX, loc[1] + moveY }, seed);
            }
            else
            {
                RpcMovePlayer(new int[] { loc[0] + moveX, loc[1] + moveY }, seed);
            }

            foreach (Piece p in flunkies)
            {
                if (CarlMath.Dist(p.loc, loc) > 6)
                {
                    int[] targetLoc = p.GetClosestMove(loc);

                    if (!isServer)
                    {
                        CmdMovePiece(p.loc, targetLoc, 0.06f);
                        MovePiece(p.loc, targetLoc, 0.06f);
                    }
                    else
                    {
                        RpcMovePiece(p.loc, targetLoc, 0.06f);
                    }
                }
            }
        }        
    }

    [Command]
    public void CmdRespawn(int[] newLoc)
    {
        RpcRespawn(newLoc);
    }

    [ClientRpc]
    public void RpcRespawn(int[] newLoc)
    {
        if (isServer || !isLocalPlayer)
            Respawn(newLoc);
    }

    public void Respawn(int[] newLoc)
    {
        MovePlayer(newLoc, -1);
        spawned = true;
        Debug.Log("Spawning Player");
    }

    [Command]
    void CmdMovePlayer(int[] newLoc, float seed)
    {
        //Fire(mouseDirection, pos, name, owner, seed);
        RpcMovePlayer(newLoc, seed);
    }

    [ClientRpc]
    void RpcMovePlayer(int[] newLoc, float seed)
    {
        if (isServer || !isLocalPlayer)
            MovePlayer(newLoc, seed);
    }

    void MovePlayer(int[] newLoc, float seed)
    {
        //Debug.Log("MovePlayer");
        if (Game.pieces.ContainsKey(newLoc))
        {
            Piece o = Game.pieces[newLoc];
            if (o.ownerNum == playerNum && isLocalPlayer)
            {
                int[] targetLoc = o.MoveRandomly();

                if (targetLoc != null)
                {
                    if (!isServer)
                    {
                        CmdMovePiece(o.loc, targetLoc, 0.06f);
                        MovePiece(o.loc, targetLoc, 0.06f);
                    }
                    else
                    {
                        RpcMovePiece(o.loc, targetLoc, 0.06f);
                    }
                }
                else
                {
                    o.Die(this);
                }
            }
            if (o.ownerNum != playerNum)
            {
                o.Die(this);
            }
        }
        if (Game.players.ContainsKey(newLoc))
        {
            Player o = Game.players[newLoc];
            o.Die(this);
        }
        try
        {
            Game.players.Add(newLoc, this);
            Game.players.Remove(loc);
            Game.LerpMove(transform, new Vector3(newLoc[0] * 2 + 1, -7, newLoc[1] * 2 + 1), 0.08f);
            if (spawned)
            {
                if (seed * 100 > 60)
                {
                    int spawnIndex = Mathf.FloorToInt(((seed - 0.6f) / 0.4f) * (spawnSets.Length - 0.1f));
                    for (int i = spawnIndex - 1; i <= spawnIndex + 1; i++)
                    {
                        if (spawnSets[spawnIndex % spawnSets.Length].n > 0)
                        {
                            spawnSets[spawnIndex % spawnSets.Length].n--;
                            GameObject newPiece = Instantiate(spawnSets[spawnIndex % spawnSets.Length].prefab);
                            newPiece.transform.position = transform.position;
                            newPiece.transform.rotation = transform.rotation;
                            Piece yeet = newPiece.GetComponent<Piece>();
                            Debug.Log("Spawning new " + yeet.type);
                            flunkies.Add(yeet);
                            Game.pieces.Add(loc, yeet);
                            yeet.loc = loc;
                            yeet.Init(this);
                            Game.LerpMove(newPiece.transform, new Vector3(loc[0] * 2 + 1, -7, loc[1] * 2 + 1), 0.08f);
                            Debug.Log("New piece added at " + loc[0] + ", " + loc[1]);
                            break;
                        }
                    }
                }
            }

            loc = newLoc; // Very important, don't forget about this!!!
        }
        catch (System.Exception e) { }
        //gameObject.transform.position = new Vector3(loc[0] * 2 + 1, -7, loc[1] * 2 + 1);
        
    }

    [Command]
    void CmdMovePiece(int[] oldLoc, int[] newLoc, float time)
    {
        //Fire(mouseDirection, pos, name, owner, seed);
        RpcMovePiece(oldLoc, newLoc, time);
    }

    [ClientRpc]
    void RpcMovePiece(int[] oldLoc, int[] newLoc, float time)
    {
        if (isServer || !isLocalPlayer)
            MovePiece(oldLoc, newLoc, time);
    }

    void MovePiece(int[] oldLoc, int[] newLoc, float time)
    {
        if (oldLoc[0] == newLoc[0] && oldLoc[1] == newLoc[1])
            return;
        if (!Game.pieces.ContainsKey(oldLoc)) return;
        Piece p = Game.pieces[oldLoc];
        p.canMove = false;
        if (Game.pieces.ContainsKey(newLoc))
        {
            Piece o = Game.pieces[newLoc];
            if (o.ownerNum == playerNum)
            {
                int[] targetLoc = o.MoveRandomly();

                if (targetLoc != null)
                {
                    if (!isServer)
                    {
                        CmdMovePiece(o.loc, targetLoc, 0.06f);
                        MovePiece(o.loc, targetLoc, 0.06f);
                    }
                    else
                    {
                        RpcMovePiece(o.loc, targetLoc, 0.06f);
                    }
                } else
                {
                    o.Die(this);
                }
            }
            if (o.ownerNum != playerNum)
            {
                o.Die(this);
            }
        }
        if (Game.players.ContainsKey(newLoc))
        {
            Player o = Game.players[newLoc];
            o.Die(this);
        }
        try
        {
            Game.pieces.Add(newLoc, p);
            Game.pieces.Remove(p.loc);
            Game.LerpMove(p.gameObject.transform, new Vector3(newLoc[0] * 2 + 1, -7, newLoc[1] * 2 + 1), time);
            p.loc = newLoc;
        }
        catch (System.Exception e) { }
        //Game.LerpMove(p.gameObject.transform, new Vector3(newLoc[0] * 2 + 1, -7, newLoc[1] * 2 + 1), time);
        //p.loc = newLoc;
        StartCoroutine(canMoveIn(p, time*2.5f));
        //p.gameObject.transform.position = new Vector3(loc[0] * 2 + 1, -7, loc[1] * 2 + 1);
    }

    public IEnumerator canMoveIn(Piece p, float time)
    {
        yield return new WaitForSeconds(time);
        p.canMove = true;
    }

    public void Die(Player consumer)
    {
        spawned = false;
        consumer.points += Game.PLAYER_WORTH;
        int[] newSpawn = new int[] { (int)Random.Range(-1000, 1000), (int)Random.Range(-1000, 1000) };
        if (!isServer)
        {
            CmdRespawn(newSpawn);
            Respawn(newSpawn);
        } else 
            RpcRespawn(newSpawn);
    }
}
