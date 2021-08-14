using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellBuilder : MonoBehaviour
{
    [SerializeField] private Cell cellPrefab;
    [SerializeField] private float localSpacing = 1;
    [SerializeField] private float fieldSpacing = 5;
    void Awake()
    {
        for (int localX = -1; localX <= 1; ++localX)
        for (int localY = -1; localY <= 1; ++localY)
        //for (int fieldX = -1; fieldX <= 1; ++fieldX)
        //for (int fieldY = -1; fieldY <= 1; ++fieldY)
        {
            int fieldX = 0;
            int fieldY = 0;
            Vector3 localPos = new Vector3(localX, localY, 0);
            Vector3 fieldPos = new Vector3(fieldX, fieldY, 0);
            Cell cell = Instantiate(cellPrefab, localPos * localSpacing + fieldPos * fieldSpacing, Quaternion.identity);
            cell.localX = localX;
            cell.localY = localY;
            cell.fieldX = fieldX;
            cell.fieldY = fieldY;
        }
    }
}
