using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    /*
     * 定義やらメモやら
     * 
     * rectangle 等間隔に分割した区画
     * rectSec rectangleの集合
     * wPoint rectSecの質点
     * section 曲線で分割された区画
     * room 部屋
     * 
     * 座標は左下を0,0とする
     * 
     * RectSec必要?
     * いらんこれ
     * 
     */

public class DungeonGenerator : MonoBehaviour {

    struct Section //ひとまず座標はコレクションに格納するものとする
    {
        Vector2Int lUP;
        public Section(Vector2Int leftUnderPosition)
        {
            lUP = leftUnderPosition;
        }

        public void GetTerrainLayer()
        {
            
        }
    }

    struct Separation//仕切り
    {
        private Vector2Int pos1;
        private Vector2Int pos2;
        public Separation(Vector2Int a, Vector2Int b) {
            pos1 = a;
            pos2 = b;
        }
        public void Separate(int[,] matrix) {
            matrix[pos1.x, pos1.y] = matrix[pos2.x, pos2.y];
        }
    }
    //#########################################################################################
    int seed = 0;
    static Vector2Int floorSize = new Vector2Int(160, 120);//1階層の広さ
    static Vector2Int splitTimes = new Vector2Int(16, 12);//分割数
    int maxDelSep = (splitTimes.x - 1) * splitTimes.y + (splitTimes.y - 1) * splitTimes.x;
    int minDelSep = 0;//最低区切り消去数
    int margine = 2;//質点を置く際に確保する余白
    //#########################################################################################

    bool[,] passableMap = new bool[floorSize.x, floorSize.y];//通行可能ならtrue, 仮の最終出力マップ
    float[,] weightMap = new float[floorSize.x, floorSize.y];//区画の影響マップ

    //
    int[,] rectangle = new int[splitTimes.x, splitTimes.y];

    //インスタンス宣言
    EffectMap effectMap = new EffectMap(floorSize.x, floorSize.y);
    EffectMap rectOperate = new EffectMap(splitTimes.x, splitTimes.y);


    // Use this for initialization
	void Start () {
        Random.InitState(seed);
        int n = 0;
        Queue<Separation> separations = new Queue<Separation>();
        rectOperate.MatrixOperate((x, y) => {
            rectangle[x, y] = n;
            n++;

            /*## ↑矩形に番号割り当て / 仕切りをコレクションに格納↓ ##*/

            for(int ang = 0; ang < 1; ang++) {
                int curx = (int)Mathf.Cos(Mathf.PI * ang / 2);
                int cury = (int)Mathf.Sin(Mathf.PI * ang / 2);
                Vector2Int curPos = new Vector2Int(curx, cury);
                if (rectOperate.WithinMapRange(curPos)) {
                    separations.Enqueue(new Separation(new Vector2Int(x, y), curPos));
                }
            }
        });

        //ランダムな回数分キューの中身を回して削除する仕切りをコレクションに格納
        Queue<Separation> DelSeps = new Queue<Separation>();
        int separateTimes = Random.Range(minDelSep, maxDelSep + 1);
        for(int i = 0; i < separateTimes; i++) {
            int rotationTimes = Random.Range(0, separations.Count);
            for(int j = 0; j < rotationTimes; j++) {
                separations.Enqueue(separations.Dequeue());
                separations.Enqueue(separations.Dequeue());
            }
            DelSeps.Enqueue(separations.Dequeue());
        }

        //仕切りを取り除く
        foreach(Separation separation in DelSeps) {
            separation.Separate(rectangle);
        }

        //rectangleをListに格納する
        List<List<Vector2Int>> rectSecs = new List<List<Vector2Int>>();
        for(int i = 0; i < splitTimes.x * splitTimes.y; i++){
            List<Vector2Int> cur = new List<Vector2Int>();
            rectOperate.MatrixOperate((x, y) => {
                if (rectangle[x, y] == i)
                {
                    cur.Add(new Vector2Int(x, y));
                }
            });
            if(cur.Count != 0) {
                rectSecs.Add(cur);
            }
        }

        //RectSec毎に質点を置く矩形を決定、マージンを取りつつ設置
        for(int i = 0; i < rectSecs.Count; i++) {
            List<Vector2Int> minDistRects = new List<Vector2Int>();
            float minDist = float.MaxValue;
            for (int j = 0; j < rectSecs[i].rects.Count; j++) {
                float distSum = 1;
                foreach(Vector2Int rectPos2 in rectSecs[i].rects) {
                    float curDist = Vector2Int.Distance(rectSecs[i].rects[j], rectPos2);
                    distSum *= (curDist != 0) ? curDist : 1;
                }
                if (minDist > distSum) {
                    minDist = distSum;
                    minDistRects.Clear();
                    minDistRects.Add(rectSecs[i].rects[j]);
                } else if (minDist == distSum) {
                    minDistRects.Add(rectSecs[i].rects[j]);
                }
            }
            //rectSecs[i].rectPoint = new Vector2Int(); 
            rectSecs.weight = 
        }
	}
	
}
