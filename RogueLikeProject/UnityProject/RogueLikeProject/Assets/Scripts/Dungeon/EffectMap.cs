using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//構造体の宣言
public struct MapMatrix//影響マップ合成のために行列と係数をセットにした
{
    public float[,] map;//行列
    public float coefficient;//係数
}

public class EffectMap : MonoBehaviour {
    
    //配列の長さ
    private Vector2Int mapRange;
    
    //コンストラクタ
    public EffectMap(int xRange, int yRange)
    {
        mapRange = new Vector2Int(xRange, yRange);
    }
    
    //デリゲートを宣言
    public delegate void MOFunction(int xcount, int ycount);
    public delegate void DMOFunction(int xcount, int ycount, int distance);
    public delegate bool JudgePosition(Vector2Int judgePosition);
    
    //複数のマップを合成する関数
    public float[,] MapFusion(List<MapMatrix> mapMatrix)
    {
        float[,] result = new float[mapRange.x, mapRange.y];

        MatrixOperate((xcount, ycount) =>
        {
            float coefficientSum = 0;
            for (int i = 0; i < mapMatrix.Count; i++)
            {
                coefficientSum += mapMatrix[i].coefficient;
                result[xcount, ycount] += mapMatrix[i].coefficient * mapMatrix[i].map[xcount, ycount];
            }
            result[xcount, ycount] /= coefficientSum;
        });
        return result;
    }

    //単純な２重for文で行列を操作する
    public void MatrixOperate(MOFunction mOFunction)
    {
        for(int x = 0; x < mapRange.x; x++)
        {
            for(int y = 0; y < mapRange.y; y++)
            {
                mOFunction(x,y);
            }
        }
    }

    //下の関数で使う構造体
    private struct SearchAgent
    {
        public Vector2Int position;
        public int distance;
    }

    //1つの座標を始点として隣に移りながら行列を操作する
    public void DetureMatrixOperate(Vector2Int basePosition, JudgePosition judgePosition, DMOFunction dMOFunction)
    {
        int[,] searchMatrix = new int[mapRange.x, mapRange.y];//呼び出し元にbasePositionからの距離（basePOsitionを1）を提供するための行列
        Queue<SearchAgent> searchAgent = new Queue<SearchAgent>();
        searchAgent.Enqueue(new SearchAgent() { position = basePosition, distance = 1 });
        searchMatrix[basePosition.x, basePosition.y] = 1;
        while (0 < searchAgent.Count)
        {
            SearchAgent current = searchAgent.Dequeue();
            NextPoint(current.position, (x,y) => 
            {
                if (searchMatrix[x,y] == 0 && judgePosition(new Vector2Int(x,y)) == true)
                {
                    searchAgent.Enqueue(new SearchAgent() { position = new Vector2Int(x,y), distance = current.distance + 1 });
                    searchMatrix[x, y] = current.distance + 1;
                }
            });
        }
        MatrixOperate((xcount, ycount) =>
        {
            dMOFunction(xcount, ycount, searchMatrix[xcount, ycount]);
        });
    }

    //座標judgePositionがmapRangeの範囲内であることを判定する
    public bool WithinMapRange(Vector2Int judgePosition)
    {
        return judgePosition.x >= 0 && judgePosition.y >= 0 && judgePosition.x < mapRange.x && judgePosition.y < mapRange.y;
    }

    //隣の4座標を操作
    public void NextPoint(Vector2Int basePos, MOFunction mOFunction)
    {
        for(int ang = 0; ang < 4; ang++)
        {
            Vector2Int judgePos = new Vector2Int(basePos.x + (int)Mathf.Cos(ang * Mathf.PI / 2), basePos.y + (int)Mathf.Sin(ang * Mathf.PI / 2));
            if(WithinMapRange(judgePos) == true)
            {
                mOFunction(judgePos.x, judgePos.y);
            }
        }
    }
}
