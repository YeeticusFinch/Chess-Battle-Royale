using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public bool canMove = true;

    // Use this for initialization
    void Start () {
        canMove = true;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Init(Player owner)
    {

        this.owner = owner;
        ownerNum = owner.playerNum;

        Color color = Game.playerColors[ownerNum];
        Color color2 = new Color(color.b, color.r, color.g);
        GetComponent<Renderer>().materials[0].SetColor("_Color", color);
        GetComponent<Renderer>().materials[0].EnableKeyword("_EMISSION");
        GetComponent<Renderer>().materials[0].SetColor("_EmissionColor", color);

        GetComponent<Renderer>().materials[1].SetColor("_Color", color2);
        GetComponent<Renderer>().materials[1].EnableKeyword("_EMISSION");
        GetComponent<Renderer>().materials[1].SetColor("_EmissionColor", color2);
        GetComponent<Renderer>().materials[1].SetFloat("_EmissionStrength", 3);
    }

    public int[] MoveRandomly()
    {
        if (moves == null || moves.Count < 1)
        {
            if (useMoveSets.Count > 0)
                foreach (string str in useMoveSets)
                    if (Game.moveSets.ContainsKey(str))
                        foreach (Game.Move m in Game.moveSets[str])
                            moves.Add(m);
        }

        System.Random r = new System.Random();
        foreach (int i in Enumerable.Range(0, moves.Count).OrderBy(x => r.Next()))
        {
            foreach (int dx in Enumerable.Range(-1, 3).OrderBy(x => r.Next()))
            {
                foreach (int dy in Enumerable.Range(-1, 3).OrderBy(x => r.Next()))
                {
                    int[] yeet = FirstLine(moves[i], dx, dy);
                    if (yeet != null)
                    {
                        return new int[] { yeet[0] + loc[0], yeet[1] + loc[1] };
                    }
                }
            }
        }

        return null;
    }

    public int[] FirstLine(Game.Move m, int dx, int dy)
    {
        int tx = 0;
        int ty = 0;
        int[] result = null;
        for (int i = 0; i < m.range; i++)
        {
            tx += m.dx * m.step * (int)Mathf.Sign(dx);
            ty += m.dy * m.step * (int)Mathf.Sign(dy);
            int r = ValidMove(new int[] { loc[0] + tx, loc[1] + ty }, m);
            if (r == MOVE_NOT_ALLOWED)
                break;
            else if (r == MOVE_NOT_ALLOWED_CAN_MOVE_FURTHER)
                continue;
            result = new int[] { tx, ty };
            break;
        }
        return result;
    }

    public void Die(Player consumer)
    {
        consumer.points += selfWorth;
        if (Game.pieces.ContainsKey(loc) && Game.pieces[loc] == this)
            Game.pieces.Remove(loc);
        canMove = false;
        StartCoroutine(Death());
    }

    public IEnumerator Death()
    {
        Color color = Color.red;
        Color color2 = Color.black;
        GetComponent<Renderer>().materials[0].SetColor("_Color", color);
        GetComponent<Renderer>().materials[0].EnableKeyword("_EMISSION");
        GetComponent<Renderer>().materials[0].SetColor("_EmissionColor", color);

        GetComponent<Renderer>().materials[1].SetColor("_Color", color2);
        GetComponent<Renderer>().materials[1].EnableKeyword("_EMISSION");
        GetComponent<Renderer>().materials[1].SetColor("_EmissionColor", color2);
        GetComponent<Renderer>().materials[1].SetFloat("_EmissionStrength", 3);

        for (int i = 0; i < 10; i++)
        {
            transform.localScale *= 0.7f;
            yield return new WaitForSeconds(0.01f);
        }

        GameObject.Destroy(this.gameObject);
    }

    public int[] GetClosestMove(int[] target)
    {
        int[] disp = { target[0] - loc[0], target[1] - loc[1] };
        int[] result = { 0, 0 };
        if (moves == null || moves.Count < 1)
        {
            if (useMoveSets.Count > 0)
                foreach (string str in useMoveSets)
                    if (Game.moveSets.ContainsKey(str))
                        foreach (Game.Move m in Game.moveSets[str])
                            moves.Add(m);
        }

        foreach (Game.Move m in moves)
        {
            result = ClosestLine(result, m, disp[0], disp[1]);
            if (result[0] == target[0] && result[1] == target[1]) break;
        }

        return new int[]{loc[0] + result[0], loc[1] + result[1]};
    }


    public int[] ClosestLine(int[] currBest, Game.Move m, int dx, int dy)
    {
        int tx = 0;
        int ty = 0;
        for (int i = 0; i < m.range; i++)
        {
            tx += m.dx * m.step * (int)Mathf.Sign(dx);
            ty += m.dy * m.step * (int)Mathf.Sign(dy);
            int r = ValidMove(new int[] { loc[0] + tx, loc[1] + ty }, m);
            if (r == MOVE_NOT_ALLOWED)
                break;
            else if (r == MOVE_NOT_ALLOWED_CAN_MOVE_FURTHER)
                continue;
            if (tx == dx && ty == dy)
            {
                currBest[0] = tx;
                currBest[1] = ty;
                break;
            }
            if (CarlMath.Dist(tx, ty, dx, dy) < CarlMath.Dist(currBest[0], currBest[1], dx, dy))
            {
                currBest[0] = tx;
                currBest[1] = ty;
            }
            if (r == MOVE_ALLOWED_CANNOT_MOVE_FURTHER)
                break;
        }
        return currBest;
    }

    private static int MOVE_NOT_ALLOWED = 0;
    private static int MOVE_ALLOWED_CAN_MOVE_FURTHER = 1;
    private static int MOVE_ALLOWED_CANNOT_MOVE_FURTHER = 2;
    private static int MOVE_NOT_ALLOWED_CAN_MOVE_FURTHER = 3;
    
    public int ValidMove(int[] pos, Game.Move m)
    {
        if (Game.pieces.ContainsKey(pos)) // Pieces
        {
            if (Game.pieces[pos].ownerNum == ownerNum) // Same color
            {
                if (m.jump)
                    return MOVE_NOT_ALLOWED_CAN_MOVE_FURTHER;
                return MOVE_NOT_ALLOWED;
            }
            else // Different color
            {
                if (m.canConsume || m.onlyForConsume)
                {
                    if (m.jump)
                        return MOVE_ALLOWED_CAN_MOVE_FURTHER;
                    return MOVE_ALLOWED_CANNOT_MOVE_FURTHER;
                }
                if (m.jump)
                    return MOVE_NOT_ALLOWED_CAN_MOVE_FURTHER;
            }
        } else if (Game.players.ContainsKey(pos)) // Kings
        {
            if (Game.players[pos].playerNum == ownerNum) // Same color
            {
                if (m.jump)
                    return MOVE_NOT_ALLOWED_CAN_MOVE_FURTHER;
                return MOVE_NOT_ALLOWED;
            }
            else // Different color
            {
                if (m.canConsume || m.onlyForConsume)
                {
                    if (m.jump)
                        return MOVE_ALLOWED_CAN_MOVE_FURTHER;
                    return MOVE_ALLOWED_CANNOT_MOVE_FURTHER;
                }
                if (m.jump)
                    return MOVE_NOT_ALLOWED_CAN_MOVE_FURTHER;
            }
        }
        else if (m.onlyForConsume)
            return MOVE_NOT_ALLOWED;
        return MOVE_ALLOWED_CAN_MOVE_FURTHER;
    }

    // Old shit
    /*
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
    }*/
}
