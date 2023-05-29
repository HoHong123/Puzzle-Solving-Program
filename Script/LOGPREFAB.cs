using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LOGPREFAB : MonoBehaviour {

    private void Awake()
    {
        this.gameObject.GetComponent<Button>().onClick.AddListener(()=> {
            if(ReadingPatternManager.S != null)
            {
                ReadingPatternManager.S.ActiveLogPrefab(this.gameObject);
            }
        });
    }
}
