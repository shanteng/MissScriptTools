using UnityEngine;
using UnityEngine.UI;

using UnityEngine.EventSystems;
using System.Collections.Generic;


public class TelephoneSimulateView : MonoBehaviour
{
    public Button mResetButton;
    public List<Text> mNumList;
    public Transform mNumEffect;

    public GameObject mTouchEffect;
    public List<Transform> mEffectRoots;

    public RectTransform mRotateTran;
   

    public GameObject mMask;

    public float mMaxAngle = 60f;
    public float mOneAngle = 0;
    public float mOneFrameOffset = 40;
    public Vector2 mRaidusRage = new Vector2(200f, 350f);
    public float mRotateBackRate = 5f;

    private int mNumIndex = -1;
    private Dictionary<int, Transform> mEffectRootDic;
    private Dictionary<int, RectTransform> mNumDragDic;
    private Dictionary<int, Vector2> mNumDragAnchorPosDic;
    private int mCurrentIndex = -1;
    private RectTransform mCurrentRect;
    private Vector2 mNormalizeAngle;
    private bool mRollBacking = false;
    private bool mEnable = true;
    private bool mIsDailingOk = false;
    void Awake()
    {
        this.mResetButton.onClick.AddListener(this.InitData);
        this.mNumDragDic = new Dictionary<int, RectTransform>();
      
        this.mNumDragAnchorPosDic = new Dictionary<int, Vector2>();
       

        mOneAngle = Vector3.Angle(this.mNumDragAnchorPosDic[1], this.mNumDragAnchorPosDic[2]);

        this.mEffectRootDic = new Dictionary<int, Transform>();
        foreach (Transform root in this.mEffectRoots)
        {
            int num = int.Parse(root.name);
            this.mEffectRootDic[num] = root;
        }

        //开始测试
        this.InitData();
    }

    public void InitData()
    {
        this.mMask.SetActive(false);
        this.mIsDailingOk = false;
        this.ResetDrager();
        this.mNumIndex = 0;
        foreach (Text txt in this.mNumList)
        {
            txt.text = "";
        }
    }

    public void ResetDrager(bool enable = true)
    {
        this.SetEnable(true);
        this.mNumEffect.gameObject.SetActive(false);
        this.mTouchEffect.SetActive(false);
        this.mRollBacking = false;
        this.mCurrentIndex = -1;
        this.mCurrentRect = null;
        this.mRotateTran.localEulerAngles = Vector3.zero;
        
    }

    public void SetEnable(bool en)
    {
        mEnable = en;
        this.mMask.SetActive(!en);
    }

    public void DisableOtherDrager(int index)
    {
       
    }

    public void OnPointerDown(PointerEventData eventData, object param)
    {
        if (mRollBacking || !this.mEnable)
            return;

        int num = (int)param;
        this.CallNumTouched(num);
    }

    public void CallNumTouched(int num)
    {
        //播放一个接触特效
        this.mTouchEffect.transform.SetParent(this.mEffectRootDic[num]);
        this.mTouchEffect.transform.localPosition = Vector2.zero;
        this.mTouchEffect.SetActive(false);
        this.mTouchEffect.SetActive(true);
    }


    public void OnBeginDrag(PointerEventData eventData, object param)
    {
        if (mRollBacking || !this.mEnable)
            return;
        mRollBacking = false;
        this.mCurrentIndex = (int)param;
        this.mCurrentRect = this.mNumDragDic[this.mCurrentIndex];
        this.mNormalizeAngle = mNumDragAnchorPosDic[this.mCurrentIndex];
        this.DisableOtherDrager(this.mCurrentIndex);
    }

    public void OnDrag(PointerEventData eventData, object param)
    {
        if (mRollBacking || !this.mEnable)
            return;
        Vector2 pos = this.mCurrentRect.anchoredPosition;
        pos.x += eventData.delta.x;
        pos.y += eventData.delta.y;
        this.mCurrentRect.anchoredPosition = pos;

        float oldAngle = this.mRotateTran.localEulerAngles.z;
        float maxAngle = this.mMaxAngle + (this.mCurrentIndex - 1) * mOneAngle;

        Vector3 c = Vector3.Cross(pos.normalized, this.mNormalizeAngle.normalized);
        float angle = Vector3.Angle(mNormalizeAngle, pos);

        if (c.z < 0)
        {
            angle = (180 - angle) + 180;
        }

        float radius = Vector2.Distance(Vector2.zero, pos);
        if (radius < this.mRaidusRage.x || radius > this.mRaidusRage.y)
            return;

        float useAngle = -angle;
        if (useAngle < -maxAngle)
            return;

        if (useAngle > 0)
            return;

        if (oldAngle > 0)
            oldAngle = oldAngle - 360;
        float absDelta = Mathf.Abs(oldAngle - useAngle);
        if (absDelta > this.mOneFrameOffset)
            return;

        this.mRotateTran.localEulerAngles = new Vector3(0, 0, useAngle);
    }



    public void CallNumDailing(int num)
    {
        this.mNumList[this.mNumIndex].text = num.ToString();
        this.mNumEffect.SetParent(this.mNumList[this.mNumIndex].transform);
        this.mNumEffect.localPosition = Vector3.zero;
        this.mNumEffect.gameObject.SetActive(false);
        this.mNumEffect.gameObject.SetActive(true);
        this.mNumIndex++;
        if (this.mNumIndex == this.mNumList.Count)
        {
            this.SetEnable(false);
            this.DisableOtherDrager(-1);
        }
    }

    public void OnEndDrag(PointerEventData eventData, object param)
    {
        if (!this.mEnable)
            return;

        Vector3 rotate = this.mRotateTran.localEulerAngles;
        float absRotate = Mathf.Abs(rotate.z-360);
        this.mIsDailingOk = absRotate >= (this.mMaxAngle/2);
        this.mRollBacking = true;
        this.DisableOtherDrager(-1);
    }

    void Update()
    {
        if (this.mRollBacking)
        {
            //回弹
            Vector3 rotate = this.mRotateTran.localEulerAngles;
            float absRotate = Mathf.Abs(rotate.z);
            if (absRotate <= mRotateBackRate)
            {
                int num = this.GetCurNum();
                this.ResetDrager();
                if(this.mIsDailingOk)
                    this.CallNumDailing(num);
            }
            else
            {
                rotate.z += this.mRotateBackRate;
                absRotate = Mathf.Abs(rotate.z - 360);
                if (absRotate <= this.mRotateBackRate)
                {
                    int num = this.GetCurNum();
                    this.ResetDrager();
                    if (this.mIsDailingOk)
                        this.CallNumDailing(num);
                }
                else
                {
                    this.mRotateTran.localEulerAngles = rotate;
                }
            }
        }
    }

    public int GetCurNum()
    {
        return this.mCurrentIndex % 10;
    }

}//end class