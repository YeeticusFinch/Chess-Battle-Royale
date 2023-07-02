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

    void CheckControls()
    {
        if (!spawned) return;

        int moveX = Input.GetAxisRaw("Horizontal") == 0 ? 0 : (int)Mathf.Sign(Input.GetAxisRaw("Horizontal"));
        int moveY = Input.GetAxisRaw("Vertical") == 0 ? 0 : (int)Mathf.Sign(Input.GetAxisRaw("Vertical"));

        if (Time.fixedTime-prevMoveTime > 0.1f)
        {
            prevMoveTime = Time.fixedTime;
            //prevMoveX = moveX;
            //prevMoveY = moveY;
            if (!isServer)
            {
                CmdMovePlayer(new int[] { loc[0] + moveX, loc[1] + moveY });
                MovePlayer(new int[] { loc[0] + moveX, loc[1] + moveY });
            }
            else
            {
                RpcMovePlayer(new int[] { loc[0] + moveX, loc[1] + moveY });
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
        MovePlayer(newLoc);
        spawned = true;
        Debug.Log("Spawning Player");
    }

    [Command]
    void CmdMovePlayer(int[] newLoc)
    {
        //Fire(mouseDirection, pos, name, owner, seed);
        RpcMovePlayer(newLoc);
    }

    [ClientRpc]
    void RpcMovePlayer(int[] newLoc)
    {
        if (isServer || !isLocalPlayer)
            MovePlayer(newLoc);
    }

    void MovePlayer(int[] newLoc)
    {
        if (Game.pieces.ContainsKey(newLoc))
        {
            Piece o = Game.pieces[newLoc];
            o.Die(this);
        }
        if (Game.players.ContainsKey(newLoc))
        {
            Player o = Game.players[newLoc];
            o.Die(this);
        }
        Game.players.Add(newLoc, this);
        Game.players.Remove(loc);
        loc = newLoc;
        //gameObject.transform.position = new Vector3(loc[0] * 2 + 1, -7, loc[1] * 2 + 1);
        Game.LerpMove(transform, new Vector3(loc[0] * 2 + 1, -7, loc[1] * 2 + 1), 0.08f);
    }

    [Command]
    void CmdMovePiece(int[] oldLoc, int[] newLoc)
    {
        //Fire(mouseDirection, pos, name, owner, seed);
        RpcMovePiece(oldLoc, newLoc);
    }

    [ClientRpc]
    void RpcMovePiece(int[] oldLoc, int[] newLoc)
    {
        if (isServer || !isLocalPlayer)
            MovePiece(oldLoc, newLoc);
    }

    void MovePiece(int[] oldLoc, int[] newLoc)
    {
        if (!Game.pieces.ContainsKey(oldLoc)) return;
        Piece p = Game.pieces[oldLoc];
        if (Game.pieces.ContainsKey(newLoc))
        {
            Piece o = Game.pieces[newLoc];
            if (o.ownerNum != playerNum || !o.MoveRandomly(newLoc[0] - oldLoc[0], newLoc[1] - oldLoc[1]))
            {
                o.Die(this);
            }
        }
        if (Game.players.ContainsKey(newLoc))
        {
            Player o = Game.players[newLoc];
            o.Die(this);
        }
        Game.pieces.Add(newLoc, p);
        Game.pieces.Remove(p.loc);
        p.loc = newLoc;
        p.gameObject.transform.position = new Vector3(loc[0] * 2 + 1, -7, loc[1] * 2 + 1);
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
