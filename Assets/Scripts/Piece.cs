using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour {

    public string type;
    public int selfWorth = -1;
    public int[] loc;
    public int ownerNum = -1;
    public Player owner;

    public List<Game.Move> moves = new List<Game.Move>();
    public List<int[]> movableLocations = new List<int[]>();
    public List<string> useMoveSets = new List<string>();

    public bool decoration = false;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Init(Player owner)
    {
        this.owner = owner;
        ownerNum = owner.playerNum;
    }

    public bool MoveRandomly(int dx, int dy)
    {
        return false;
    }

    public void Die(Player consumer)
    {
        consumer.points += selfWorth;
        if (Game.pieces.ContainsKey(loc) && Game.pieces[loc] == this)
            Game.pieces[loc] = null;
        GameObject.Destroy(this);
    }

    // int lateral, int diagonal = 0, bool onlyForward = false, bool force = false, bool jump = false, bool canConsume = true, bool onlyForConsume = false, bool firstMove = false
    public List<int[]> GetMovableLocations()
    {
        movableLocations = new List<int[]>();
        List<int[]> result = new List<int[]>();
        if (moves == null || moves.Count < 1)
        {
            if (useMoveSets.Count > 0)
                foreach (string str in useMoveSets)
                    if (Game.moveSets.ContainsKey(str))
                        foreach (Game.Move m in Game.moveSets[str])
                            moves.Add(m);
        }
        if (moves == null || moves.Count < 1) return null;
        Debug.Log("Calculating moves for " + type);
        foreach (Game.Move m in moves)
        {
            //if (m.firstMove && hasMoved) continue;
            if (m.lateral != 0)
            {
                for (int d = 0; d < 2; d++) // iterate over dimensions
                {
                    //Debug.Log("Checking dim " + d);
                    LatSlideCheck(result, m, loc, d, m.step);
                    LatSlideCheck(result, m, loc, d, -m.step);
                }
            }
            else if (m.diagonal != 0)
            {
                for (int d = 0; d < 2; d++) // iterate over dimensions
                {
                    for (int dd = 0; dd < 2; dd++) //iterate over perpendicular dimensions
                    {
                        if (dd == d) continue;
                        
                        DiaSlideCheck(result, m, loc, d, dd, m.step, m.step);
                        DiaSlideCheck(result, m, loc, d, dd, -m.step, m.step);
                        DiaSlideCheck(result, m, loc, d, dd, m.step, -m.step);
                        DiaSlideCheck(result, m, loc, d, dd, -m.step, -m.step);
                        
                    }
                }
            }
        }
        return null; // idk what's the point of this lmao
    }

    // addTo = list to add moves to    m = current move,   startLoc = location to check from,   d = dimension,    step = iteration step (positive or negative)
    private List<int[]> LatSlideCheck(List<int[]> addTo, Game.Move m, int[] startLoc, int d, int step)
    {
        int[] newLatLoc = new int[] { startLoc[0], startLoc[1] };
        //Debug.Log("Lat Slide Check " + Random.Range(0, 100) + " : " + newLatLoc[0] + ", " + newLatLoc[1]);
        while (m.lateral == -1 || Mathf.Abs(newLatLoc[d] - loc[d]) < m.lateral)
        {
            newLatLoc[d] += step;
            //Debug.Log("LatLoop " + Random.Range(0, 100) + " : " + newLatLoc[0] + ", " + newLatLoc[1]);
            if (m.force && Mathf.Abs(newLatLoc[d] - loc[d]) != m.lateral) continue;
            if (m.diagonal != 0)
            {
                for (int dd = 0; dd < 2; dd++) // iterate over dimensions for diagonal movement
                {
                    if (dd == d) continue;
                    DiaSlideCheck(addTo, m, newLatLoc, d, dd, step, step);
                    DiaSlideCheck(addTo, m, newLatLoc, d, dd, step, -step);
                }
            }
            else
            {
                int r = AttemptAddMove(addTo, m, newLatLoc);
                if (r == 1 || r == 3) break;
            }
        }
        return addTo;
    }

    // addTo = list to add moves to   m = current move,   startLoc = location to check form,   d = fixed component dimension,   dd = perpendicular dimension,   step = fixed component iteration step,   step2 = perpendicular iteration step
    private List<int[]> DiaSlideCheck(List<int[]> addTo, Game.Move m, int[] startLoc, int d, int dd, int step, int step2)
    {
        //Debug.Log("Dia Slide Check");
        int[] newDiaLoc = new int[] { startLoc[0], startLoc[1] };
        while ((m.diagonal == -1 || Mathf.Abs(newDiaLoc[dd] - loc[dd]) < m.diagonal))
        {
            newDiaLoc[d] += step;
            newDiaLoc[dd] += step2;
            if (m.force && Mathf.Abs(newDiaLoc[dd] - loc[dd]) != m.diagonal) continue;
            int r = AttemptAddMove(addTo, m, newDiaLoc);
            if (r == 1 || r == 3) break;
            //addTo.Add(new List<int>(newDiaLoc));
        }
        return addTo;
    }

    // 0 = move allowed, no piece on square, CAN move further
    // 1 = move allowed, consumed piece on square, CANNOT move further
    // 2 = move allowed, consumed piece on square, or CAN move further
    // 3 = move not allowed, piece on square, CANNOT move further
    // 4 = move not allowed, piece on square, CAN move further
    private int AttemptAddMove(List<int[]> addTo, Game.Move m, int[] loc)
    {
        Debug.Log("Adding move");
        //string str = CarlMath.ListAsString(loc);
        //board.squares[str].TempColor(Color.blue);
        //ChessPiece piece = board.PieceAt(loc);
        GameObject piece = null;
        int targetOwnerNum = -1;
        if (Game.pieces.ContainsKey(loc))
        {
            piece = Game.pieces[loc].gameObject;
            targetOwnerNum = Game.pieces[loc].ownerNum;
        }
        else if (Game.players.ContainsKey(loc))
        {
            piece = Game.players[loc].gameObject;
            targetOwnerNum = Game.players[loc].playerNum;
        }

        if (piece == null)
        {
            if (m.onlyForConsume) return 4;
            //board.squares[str].TempColor(Color.green);
            //addTo.Add(new List<int>(loc));
            //movableLocations.Add(board.squares[str]);
            Debug.Log("added move to moveable locations");
            return 0;
        }
        if ((targetOwnerNum != ownerNum) && (m.canConsume || m.onlyForConsume))
        {
            //board.squares[str].TempColor(Color.green);
            //addTo.Add(new List<int>(loc));
            //movableLocations.Add(board.squares[str]);
            Debug.Log("added move to moveable locations");
            if (m.jump) return 2;
            return 1;
        }
        //board.squares[str].TempColor(Color.black);
        if (m.jump) return 4;
        return 3;
    }
}
