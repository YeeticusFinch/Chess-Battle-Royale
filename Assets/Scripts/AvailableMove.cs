using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvailableMove : MonoBehaviour {

    public int[] loc = { 0, 0 };
    public Piece pieceOwner;
    public Player playerOwner;
    public int ownerNum = -1;

    public void Init(int[] loc, Piece pieceOwner)
    {
        this.loc = loc;
        this.pieceOwner = pieceOwner;
        playerOwner = pieceOwner.owner;
        ownerNum = pieceOwner.ownerNum;
        transform.position = new Vector3(loc[0] * 2 + 1, -7, loc[1] * 2 + 1);
    }

    // Use this for initialization
    void Start () {

    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
