using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UIGrid))]
public class LoopTool : MonoBehaviour
{
    public void Awake()
    {
        isInit = false;
    }

    private IEnumerator OnCenterChild(GameObject go)
    {
        yield return new WaitForSeconds(0.05f);
        if (go != null) SetCenter();
    }

    public void SetCenter()
    {
        if (mCenterObj != null)
        {
            mCenter = GetComponent<UICenterOnChild>();
            if (mCenter != null) mCenter.CenterOn(mCenterObj.transform);
        }
    }

    #region 成员

    /// <summary>
    ///     存储物件列表
    /// </summary>
    private readonly List<GameObject> mItemList = new List<GameObject>();

    private readonly BetterList<GameObject> mHideItemList = new BetterList<GameObject>();

    private Vector4 mPosParam;
    private Transform mCachedTransform;

    /// <summary>
    ///     起始下标
    /// </summary>
    public int mStartIndex;
    //最大的起始下标
    private int mStartMaxIndex;

    /// <summary>
    ///     最大长度
    /// </summary>
    public int mMaxCount;

    /// <summary>
    ///     物件刷新代理事件
    /// </summary>
    /// <param name="go"></param>
    public delegate void OnItemChange(GameObject go, int index);

    private OnItemChange mItemChangeCallBack;

    /// <summary>
    ///     item点击代理事件
    /// </summary>
    /// <param name="go"></param>
    /// <param name="i"></param>
    public delegate void OnClickItem(GameObject go, int i);

    private OnClickItem mOnClickItemCallBack;

    public GameObject mItemModel;

    /// <summary>
    ///     父ScrollView
    /// </summary>
    public UIScrollView mScrollView;

    public UIGrid mGrid;

    public UICenterOnChild mCenter;

    public GameObject mCenterObj;

    public bool isInit;

    public bool IsCenter = false;

    /// <summary>
    ///     是否开始检查
    /// </summary>
    private bool isCheck = true;

    public float mCullX;
    public float mCullY;

    /// <summary>
    ///     适应的item数量
    /// </summary>
    public int mFitNumb;

    public List<GameObject> GetItemList
    {
        get { return mItemList; }
    }

    #endregion

    #region 核心逻辑

    /// <summary>
    ///     初始化工具，必须调用一次
    /// </summary>
    /// <param name="onlick">item是否有点击回调</param>
    public void Init(int max, bool onlick)
    {
        mCachedTransform = transform;
        mScrollView = mCachedTransform.parent.GetComponent<UIScrollView>();

        mScrollView.panel.onClipMove = Move;

        // 设置Cull  
        mScrollView.GetComponent<UIPanel>().cullWhileDragging = true;

        //获取UIGrid的单个元素的宽高用来计算我的UIScrollView需要几个元素
        mGrid = GetComponent<UIGrid>();
        var _cellWidth = mGrid.cellWidth;
        var _cellHeight = Mathf.Abs(mGrid.cellHeight);

        mPosParam = new Vector4(_cellWidth, _cellHeight,
            mGrid.arrangement == UIGrid.Arrangement.Horizontal ? 1 : 0,
            mGrid.arrangement == UIGrid.Arrangement.Vertical ? 1 : 0);
        //mPosParam {100,580,0,1}
        
        //取得UIPanel的剪裁范围
        mCullX = mScrollView.panel.baseClipRegion.z;
        mCullY = mScrollView.panel.baseClipRegion.w;
        /**
         * baseClipRegion.z = Size.x
         * baseClipRegion.w = Size.y
         * baseClipRegion.x = Center.x
         * baseClipRegion.y = Center.y
         */

        Debug.Log(string.Format("mClullX:{0},mCullY:{1}",mCullX,mCullY));

        //预存两个用来刷新，避免出现有视觉差
        if (mCullX * mPosParam.z > 0)
            mFitNumb = (int) Mathf.Ceil(mCullX / _cellWidth + 2);
        else if (mCullY * mPosParam.w > 0) mFitNumb = (int) Mathf.Ceil(mCullY / _cellHeight + 2);
        // mFitNumb = 6
        mHideItemList.Clear();

        //获取Grid下的第一个GameObject 作为原材料
        for (var i = 0; i < mCachedTransform.childCount; i++)
        {
            var item = mCachedTransform.GetChild(i).gameObject;
            if (item == null) return;

            if (mItemModel == null)
            {
                mItemModel = item;
                item.gameObject.SetActive(false);
                continue;
            }

            if (item == mItemModel) continue;

            mHideItemList.Add(item);
        }


        if (mHideItemList.size == 0 && mItemModel == null) return;
        isInit = true;

        SetMaxCount(max);
        SetMaxStartIndex(mMaxCount - mFitNumb + 2);
        

        ReSet(onlick);

        if (mFitNumb > max)
            mScrollView.disableDragIfFits = true;
        else
            mScrollView.disableDragIfFits = false;
    }

    /// <summary>
    ///     重置无线滚动条
    /// </summary>
    /// <param name="isOnclick"></param>
    public void ReSet(bool isOnclick)
    {
        if (!isInit)
        {
            Debug.Log("没有初始化");
            return;
        }

        try
        {
            mScrollView.panel.onClipMove = null;
            ClearItem();

            #region ScrollView 的Panel能够装下所有元素
            if (mFitNumb >= mMaxCount)
            {
                for (var i = 0; i < mMaxCount; i++)
                {
                    var item = GetItemOne(isOnclick);
                    item.gameObject.name = i.ToString();

                    if (mItemChangeCallBack != null) mItemChangeCallBack(item, i);
                    item.transform.SetSiblingIndex(i);
                    mItemList.Add(item);
                }

                if (mGrid == null) mGrid = GetComponent<UIGrid>();

                isCheck = false;
                mScrollView.restrictWithinPanel = true;
                mGrid.Reposition();
                mScrollView.ResetPosition();
                mScrollView.panel.onClipMove = Move;
                return;
            }
            #endregion

            if (mGrid == null) mGrid = GetComponent<UIGrid>();

            #region 核心逻辑

            mStartIndex = Math.Min(mStartIndex, mStartMaxIndex);
            Debug.Log("mStartMaxIndex" + mStartMaxIndex);

            //核心逻辑
            for (var i = 0 + mStartIndex; i < mFitNumb + mStartIndex; i++)
            {
                int index = i;
                GameObject item = GetItemOne(isOnclick);
                if (mItemChangeCallBack != null) mItemChangeCallBack(item, index);
                item.gameObject.name = index.ToString();
                item.transform.SetSiblingIndex(index);
                mItemList.Add(item);

                if (index >= mMaxCount) item.SetActive(false);

                if (index == mStartIndex && IsCenter) mCenterObj = item;
            }
            #endregion


            if (mGrid == null) mGrid = GetComponent<UIGrid>();

            mScrollView.restrictWithinPanel = false;
            mGrid.Reposition();
            mScrollView.ResetPosition();


            if (mCenterObj != null)
            {
                mCenter = GetComponent<UICenterOnChild>();
                if (mCenter != null) StartCoroutine(OnCenterChild(mCenter.gameObject));
            }


            isCheck = true;
            mScrollView.panel.onClipMove = Move;
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + e + "         " + e.HelpLink + "          " + e.StackTrace + "   " + e.Source);
        }
    }

    private GameObject GetItemOne(bool isOnclick)
    {
        GameObject item = null;

        if (mHideItemList.size > 0)
        {
            item = mHideItemList.Pop();
            item.gameObject.SetActive(true);
        }
        else
        {
            item = Instantiate(mItemModel);
            item.gameObject.SetActive(true);
            item.transform.SetParent(mCachedTransform);
            item.transform.localScale = Vector3.one;
        }

        if (isOnclick)
        {
            // 允许响应点击事件  
            var box = item.GetComponent<Collider>();
            if (box == null)
            {
                box = item.gameObject.AddComponent<BoxCollider>();

                var wight = item.GetComponent<UIWidget>();
                if (wight == null) wight = item.AddComponent<UIWidget>();
                wight.autoResizeBoxCollider = true;
            }

            // 事件接收  
            var _listener = item.GetComponent<UIEventListener>();
            if (_listener == null) _listener = item.gameObject.AddComponent<UIEventListener>();

            // 点击回调  
            _listener.onClick = OnClickListItem;
        }

        return item;
    }
    
    
    private void ClearItem()
    {
        for (var i = 0; i < mItemList.Count; i++)
        {
            if (!mHideItemList.Contains(mItemList[i])) mHideItemList.Add(mItemList[i]);
            mItemList[i].SetActive(false);
        }

        mItemList.Clear();
    }


    private void Move(UIPanel panel)
    {
        if (mItemList.Count <= 1 || !isCheck) return;

        var startIndex = -1;
        var targetIndex = -1;
        var temp = 0;

        mGrid = GetComponent<UIGrid>();
        var hight = mGrid.cellHeight;

        UIWidget wight1;
        UIWidget wight2;
        if (hight > 0)
        {
            //取得UIPanel上的元素 尾部复用元素
            wight1 = mItemList[0].GetComponent<UIWidget>();
            if (wight1 == null) wight1 = mItemList[0].AddComponent<UIWidget>();
            //取得UIPanel下的元素 头部复用元素
            wight2 = mItemList[mItemList.Count - 1].GetComponent<UIWidget>();
            if (wight2 == null) wight2 = mItemList[mItemList.Count - 1].AddComponent<UIWidget>();
        }
        else
        {
            wight1 = mItemList[mItemList.Count - 1].GetComponent<UIWidget>();
            if (wight1 == null) wight1 = mItemList[mItemList.Count - 1].AddComponent<UIWidget>();
            wight2 = mItemList[0].GetComponent<UIWidget>();
            if (wight2 == null) wight2 = mItemList[0].AddComponent<UIWidget>();
        }

        var firstVislable = wight1.isVisible;
        var lastVisiable = wight2.isVisible;

        Debug.LogFormat("{0}:{1}",firstVislable,lastVisiable);

        if (!firstVislable && !lastVisiable)
        {
            mScrollView.RestrictWithinBounds(false);
        }


        // 如果都显示,那么返回  
        if (firstVislable == lastVisiable) return;

        // 得到需要替换的源和目标  
        if (firstVislable)
        {
            if (hight > 0)
            {
                startIndex = mItemList.Count - 1;
                targetIndex = 0;
                temp = 1;
            }
            else //适应gird的高度为负数
            {
                startIndex = 0;
                targetIndex = mItemList.Count - 1;
                temp = -1;
            }
        }
        else if (lastVisiable)
        {
            if (hight > 0)
            {
                startIndex = 0;
                targetIndex = mItemList.Count - 1;
                temp = -1;
            }
            else //适应gird的高度为负数
            {
                startIndex = mItemList.Count - 1;
                targetIndex = 0;
                temp = 1;
            }
        }

        // 如果小于真正的初始索引或大于真正的结束索引,返回  
        var realSourceIndex = int.Parse(mItemList[startIndex].gameObject.name);
        var realTargetIndex = int.Parse(mItemList[targetIndex].gameObject.name);

        /*if (realTargetIndex <= mStartIndex ||
            realTargetIndex >= (mMaxCount - 1))
        {
            mScrollView.restrictWithinPanel = true;
            return;
        }*/

        if (realTargetIndex <= 0 || realTargetIndex >= mMaxCount - 1)
        {
            mScrollView.restrictWithinPanel = true;
            return;
        }

        mScrollView.restrictWithinPanel = false;
        var item = mItemList[startIndex];

        Vector3 _offset = new Vector2(temp * mPosParam.x * mPosParam.z,
            temp * mPosParam.y * mPosParam.w);

        if (hight > 0) //适应gird的高度为负数
            item.transform.localPosition = mItemList[targetIndex].transform.localPosition + _offset;
        else
            item.transform.localPosition = mItemList[targetIndex].transform.localPosition - _offset;

        mItemList.RemoveAt(startIndex);
        mItemList.Insert(targetIndex, item);

        var index = realSourceIndex > realTargetIndex ? realTargetIndex - 1 : realTargetIndex + 1;
        item.name = index.ToString();

        item.SetActive(true);
        OnItemChangeMsg(item, index);
       
    }

    /// <summary>
    ///     item点击回调
    /// </summary>
    /// <param name="go"></param>
    private void OnClickListItem(GameObject go)
    {
        var _i = int.Parse(go.name);

        if (mOnClickItemCallBack != null) mOnClickItemCallBack(go, _i);
    }

    /// <summary>
    ///     item信息改变回调
    /// </summary>
    /// <param name="go"></param>
    /// <param name="index"></param>
    private void OnItemChangeMsg(GameObject go, int index)
    {
        if (mItemChangeCallBack != null) mItemChangeCallBack(go, index);
    }

    #endregion

    #region 外部接口

    /// <summary>
    ///     设置代理
    /// </summary>
    /// <param name="_onItemChange"></param>
    /// <param name="_onClickItem"></param>
    public void SetDelegate(OnItemChange _onItemChange,
        OnClickItem _onClickItem)
    {
        mItemChangeCallBack = _onItemChange;

        // Debug.LogError("-----" + _onClickItem);

        if (_onClickItem != null) mOnClickItemCallBack = _onClickItem;
    }

    /// <summary>
    ///     获得所有item的某个组件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public List<T> GetItemCompoent<T>() where T : Component
    {
        var _list = new List<T>();

        for (var i = 0; i < mItemList.Count; i++) _list.Add(mItemList[i].GetComponent<T>());

        return _list;
    }

    /// <summary>
    ///     得到所有item孩子的的某个组件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public List<T> GetItemListInChildren<T>() where T : Component
    {
        var _list = new List<T>();

        for (var i = 0; i < mItemList.Count; i++) _list.Add(mItemList[i].GetComponentInChildren<T>());

        return _list;
    }

    /// <summary>
    ///     更新表单
    /// </summary>
    /// <param name="_count">数量</param>
    public void SetMaxCount(int _count)
    {
        if (mStartIndex == 0) mStartIndex = 0;

        mMaxCount = _count;
    }

    public void SetMaxStartIndex(int index)
    {
        mStartMaxIndex = index;
    }

    public void SetStartIndex(int index)
    {
        mStartIndex = index;
    }

    /// <summary>
    ///     移除一个Item
    /// </summary>
    /// <param name="index">item位置</param>
    public void RemoveItem(int index)
    {
        mMaxCount--;

        for (var i = 0; i < mItemList.Count; i++)
        {
            var temp = int.Parse(mItemList[i].gameObject.name);
            if (temp >= index) OnItemChangeMsg(mItemList[i], temp);
        }
    }

    /// <summary>
    ///     移除一个item
    /// </summary>
    /// <param name="item"></param>
    public void RemoveItem(GameObject item)
    {
        mMaxCount--;

        var temp1 = int.Parse(item.gameObject.name);

        for (var i = 0; i < mItemList.Count; i++)
        {
            var temp2 = int.Parse(mItemList[i].gameObject.name);
            if (temp2 >= temp1) OnItemChangeMsg(mItemList[i], temp2);
        }
    }

    /// <summary>
    ///     插入一个item
    /// </summary>
    /// <param name="index"></param>
    public void InsetItem(int index)
    {
        mMaxCount++;
        for (var i = 0; i < mItemList.Count; i++)
        {
            var tempIndex = int.Parse(mItemList[i].gameObject.name);
            if (tempIndex >= index) OnItemChangeMsg(mItemList[i], tempIndex);
        }
    }

    /// <summary>
    ///     得到当前显示的Item的下表
    /// </summary>
    /// <returns></returns>
    public List<int> GetCurIndex()
    {
        var temp = new List<int>();

        for (var i = 0; i < mItemList.Count; i++)
        {
            var index = int.Parse(mItemList[i].transform.name);
            temp.Add(index);
        }

        return temp;
    }

    /// <summary>
    ///     根据下表找item
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public GameObject FindItem(string index)
    {
        for (var i = 0; i < mItemList.Count; i++)
            if (mItemList[i].gameObject.name == index)
                return mItemList[i];
        return null;
    }

    #endregion
}