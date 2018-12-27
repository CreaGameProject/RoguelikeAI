using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EffectMap : MonoBehaviour {

    //調整可能な値
    [SerializeField] GameObject enemy;//敵オブジェクト
    [SerializeField] GameObject item;//アイテムオブジェクト
    [SerializeField] Vector2Int enemyPos;//敵座標
    [SerializeField] Vector2Int itemPos;//アイテム座標
    [SerializeField] int enemyCoefficient;//敵のマップの係数
    [SerializeField] int itemCoefficient;//アイテムのマップの係数
    
    //変数、配列宣言
    private Vector2Int mapRange;//行列の範囲
    private bool[,] passableMap;//床true 壁false
    private GameObject[,] terrains;//マップのオブジェクト格納

    //デリゲートを宣言
    private delegate void MOFunction(int xcount, int ycount);
    private delegate void DMOFunction(int xcount, int ycount, int distance);
    private delegate bool JudgePosition(Vector2Int judgePosition);

    //構造体の宣言
    private struct MapMatrix//影響マップ合成のために行列と係数をセットにした
    {
        public float[,] map;//行列
        public float coefficient;//係数
    }

    //結果を格納する行列を宣言
    private float[,] effectMap;//最終的な影響マップ

	// Use this for initialization
	void Start () {
            //オブジェクトのInstantiate
        enemy = Instantiate(enemy, new Vector3(enemyPos.x, 0, enemyPos.y), Quaternion.identity);
        item =Instantiate(item, new Vector3(itemPos.x, 0, itemPos.y), Quaternion.identity);
            //mapGeneratorのインスタンス格納、変数、配列の読み込み
        MapGenerator mapGenerator = gameObject.GetComponent<MapGenerator>();
        passableMap = mapGenerator.passableMap;
        terrains = mapGenerator.terrains;
        mapRange = mapGenerator.mapRange;
            //結果を格納する行列を初期化
        effectMap = new float[mapRange.x, mapRange.y];
	}

    //影響マップ生成関数の呼び出し、オブジェクト座標の反映
    private void Update()
    {
        enemy.GetComponent<Transform>().position = new Vector3(enemyPos.x, 0, enemyPos.y);
        item.GetComponent<Transform>().position = new Vector3(itemPos.x, 0, itemPos.y);
        EffectMapping();
    }

    //最終影響マップ生成
    private void EffectMapping()
    {
        MapMatrix enemyMap = new MapMatrix { map = EnemyMapping(), coefficient = enemyCoefficient };
        MapMatrix itemMap = new MapMatrix { map = ItemMapping(), coefficient = itemCoefficient };
        effectMap = MapFusion(new List<MapMatrix> { enemyMap, itemMap });
        MatrixOperate((xcount, ycount) =>
        {
            if(terrains[xcount, ycount].GetComponent<Renderer>().material.color != Color.gray)
            {
                terrains[xcount, ycount].GetComponent<Renderer>().material.color = new Color(effectMap[xcount, ycount], 0, 0);
                terrains[xcount, ycount].transform.GetChild(0).GetChild(0).GetComponent<Text>().text = effectMap[xcount, ycount].ToString("f3");
            }
        });
    }

    //敵の影響マップ生成関数
    private float[,] EnemyMapping()
    {
        float[,] result = new float[mapRange.x, mapRange.y];
        DetureMatrixOperate(enemyPos, (judge) =>
        {
            return passableMap[judge.x, judge.y] == true;
        },
        (xcount, ycount, distance) =>
        {
            if(distance > 1)
            {
                result[xcount, ycount] = 1.0f - (float)1 / (distance - 1);
            }
        });
        return result;
    }

    //アイテムの影響マップ生成関数
    private float[,] ItemMapping()
    {
        float[,] result = new float[mapRange.x, mapRange.y];
        DetureMatrixOperate(itemPos, (judge) => 
        {
            return passableMap[judge.x, judge.y] == true;
        },
        (xcount, ycount, distance) => 
        {
            if(distance > 1)
            {
                result[xcount, ycount] = (float)1 / (distance - 1);
            }
        });
        return result;
    }

    //複数のマップを合成する関数
    private float[,] MapFusion(List<MapMatrix> mapMatrix)
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
    private void MatrixOperate(MOFunction mOFunction)
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
    private void DetureMatrixOperate(Vector2Int basePosition, JudgePosition judgePosition, DMOFunction dMOFunction)
    {
        int[,] searchMatrix = new int[mapRange.x, mapRange.y];
        Queue<SearchAgent> searchAgent = new Queue<SearchAgent>();
        searchAgent.Enqueue(new SearchAgent() { position = basePosition, distance = 1 });
        searchMatrix[basePosition.x, basePosition.y] = 1;
        while (0 < searchAgent.Count)
        {
            SearchAgent current = searchAgent.Dequeue();
            for (float rad = 0; rad < 2 * Mathf.PI; rad += Mathf.PI / 2)
            {
                Vector2Int judge = current.position;
                judge.x += (int)Mathf.Cos(rad);
                judge.y += (int)Mathf.Sin(rad);
                if(withinMapRange(judge) == true)
                {
                    if (searchMatrix[judge.x, judge.y] == 0 && judgePosition(judge) == true)
                    {
                        searchAgent.Enqueue(new SearchAgent() { position = judge, distance = current.distance + 1 });
                        searchMatrix[judge.x, judge.y] = current.distance + 1;
                    }
                }
            }
        }
        MatrixOperate((xcount, ycount) =>
        {
            dMOFunction(xcount, ycount, searchMatrix[xcount, ycount]);
        });
    }

    //座標judgePositionがmapRangeの範囲内であることを判定する
    private bool withinMapRange(Vector2Int judgePosition)
    {
        return judgePosition.x >= 0 && judgePosition.y >= 0 && judgePosition.x < mapRange.x && judgePosition.y < mapRange.y;
    }
}
