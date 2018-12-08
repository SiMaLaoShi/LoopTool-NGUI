using UnityEngine;
using System.Collections;

public class LoopTools : MonoBehaviour
{
    public LoopTool MLoopTool;
    public int Length = 10;
    public int StartIndex = 10;

    void Start()
    {
        InitLoop(0);
    }

    private void OnGUI()
    {
        
        if (GUI.Button(new Rect(0, 0, 100, 50), "到第" + StartIndex + "个位置"))
        {
            InitLoop(StartIndex);
        }
        if (GUI.Button(new Rect(0, 50, 100, 50), "移除第一个元素"))
        {
            MLoopTool.RemoveItem(0);
        }

        if (GUI.Button(new Rect(0, 100, 100, 50), "增加一个元素"))
        {
            MLoopTool.InsetItem(Length + 1);
        }
        if (GUI.Button(new Rect(0, 150, 100, 50), "定位到底部元素"))
        {
            InitLoop(Length);
        }

    }

    void InitLoop(int startIndex)
    {
        MLoopTool.SetDelegate(delegate(GameObject go, int index)
        {
            ItemModel model = new ItemModel();
            model.index = go.transform.Find("Index").GetComponent<UILabel>();
            model.index.text = "M" + index;
        }, null);
        MLoopTool.mStartIndex = startIndex;
        /*int max = 3;
        if (MLoopTool.mStartIndex >= (Length - max))
        {
            MLoopTool.mStartIndex -= max;
        }*/

        MLoopTool.Init(Length, false);
    }
}

class ItemModel
{
    public UILabel index;
}