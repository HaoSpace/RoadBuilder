using UnityEngine;
using System.Collections;
using CGEngine.Memory;

public class Home_Building : MonoBehaviour
{
    //底部區塊結構
    private struct RBottomBlock
    {
        public int Row;           //列
        public int Col;           //行
        public byte Value;        //面積值
        public Renderer Renderer; //渲染物件

        public RBottomBlock (int vRow, int vCol, byte vValue, Renderer vRenderer)
        {
            Row = vRow;
            Col = vCol;
            Value = vValue;
            Renderer = vRenderer;
        }
    }

    private OUIFM_Home mHome = null;             //主介面
    private Collider mCollider = null;           //碰撞盒
    private Transform mBuilding = null;          //建築物
    private Transform mBottom = null;            //底部
    private Transform mAnchor = null;            //錨點
    private Transform mPanel = null;             //建築介面
    private Renderer[] mBuildingRenderer = null; //建築物渲染物件
    private Renderer[] mBottomRenderer = null;   //底部渲染物件
    private bool mIsEdit = false;                //編輯標記
    private byte mID = 0;                        //建築物編號
    private byte mToward = 0;                    //建築朝向
    private eArea mArea = eArea.NONE;            //面積索引
    private RBottomBlock[,] mAreas = null;       //底部區塊資料

    public OUIFM_Home Home {get{return mHome;} set{SetHome(value);}}
    public Collider Collider{get{return mCollider;}}
    public Transform Building {get{return mBuilding;}}
    public Transform Bottom {get{return mBottom;}}
    public Transform Anchor {get{return mAnchor;} set{SetAnchor(value);}}
    public Transform Panel {get{return mPanel;} set{SetPanel(value);}}
    public bool IsEdit {get{return mIsEdit;}}
    public int Hash {get{return this.GetHashCode();}}
    public byte ID {get{return mID;} set{SetID(value);}}
    public byte Toward {get{return mToward;} set{SetToward(value);}}
    public eArea Area {get{return mArea;} set{SetArea(value);}}

    void Awake ()
    {
        //取得建築物物件
        mBuilding = transform.Find("Building");

        //取得底部物件
        mBottom = transform.Find("Bottom");

        //取得碰撞盒
        mCollider = transform.GetComponent<Collider>();

        //取得建築渲染物件
        mBuildingRenderer = transform.Find("Building").GetComponentsInChildren<Renderer>();

        //取得底部渲染物件
        mBottomRenderer = transform.Find("Bottom").GetComponentsInChildren<Renderer>();
    }

    // Update is called once per frame
    void Update ()
    {
        //更新建築介面
        Update_Panel();
    }

    //更新建築介面
    private void Update_Panel ()
    {
        //檢查編輯標記
        if (mIsEdit == false)
            return;

        //檢查介面物件
        if (mPanel == null)
            return;

        //取得視界座標
        Vector3 vPos = mHome.Camera.Camera.WorldToViewportPoint(transform.position);

        //更新座標
        mPanel.localPosition = new Vector3(vPos.x * 960f, vPos.y * 640f, 0f) - new Vector3(480f, 320f, 0f);
    }

    //檢查是否可以升起建築物
    private bool CheckCanBuildingRaiseUp ()
    {
        if (mIsEdit == true)
            return false;

        if (mHome.Building != null)
            return false;

        return true;
    }

    //檢查是否可以放下建築物
    private bool CheckCanBuildingLayDown ()
    {
        if (mIsEdit == false)
            return false;

        if (mAnchor == null)
            return false;

        int vIndex1 = GetBlockIndex1(mAnchor.name);
        int vIndex2 = GetBlockIndex2(mAnchor.name);

        return CheckHaveSpace(vIndex1, vIndex2, mAreas);
    }

    //檢查是否可以旋轉建築
    private bool CheckCanBuildingRotation ()
    {
        if (mIsEdit == false)
            return false;

        return true;
    }

    //檢查地圖範圍
    private bool CheckIsOverMapRange (int vIndex1, int vIndex2)
    {
        if ((vIndex1 <= -1) || (Const_Home.MAX_ROW <= vIndex1))
            return true;

        if ((vIndex2 <= -1) || (Const_Home.MAX_COL <= vIndex2))
            return true;

        return false;
    }

    //檢查是否有空間(方向1)
    private bool CheckHaveSpace1 (int vIndex1, int vIndex2, RBottomBlock[,] vArea)
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (vArea[i, j].Value == 0)
                    continue;

                int zIndex1 = vIndex1 + i;
                int zIndex2 = vIndex2 + j;

                //檢查地圖範圍
                if (CheckIsOverMapRange(zIndex1, zIndex2) == true)
                    return false;

                RMapBlock vBlock = mHome.Scene.Maps[zIndex1, zIndex2];

                if (vBlock.IsUsed == true)
                    return false;
            }
        }

        return true;
    }

    //檢查是否有空間(方向2)
    private bool CheckHaveSpace2 (int vIndex1, int vIndex2, RBottomBlock[,] vArea)
    {
        for (int j = 0; j < 3; j++)
        {
            for (int i = 0; i < 3; i++)
            {
                if (vArea[i, j].Value == 0)
                    continue;

                int zIndex1 = vIndex1 + j;
                int zIndex2 = vIndex2 - i;

                //檢查地圖範圍
                if (CheckIsOverMapRange(zIndex1, zIndex2) == true)
                    return false;

                RMapBlock vBlock = mHome.Scene.Maps[zIndex1, zIndex2];

                if (vBlock.IsUsed == true)
                    return false;
            }
        }

        return true;
    }

    //檢查是否有空間(方向3)
    private bool CheckHaveSpace3 (int vIndex1, int vIndex2, RBottomBlock[,] vArea)
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (vArea[i, j].Value == 0)
                    continue;

                int zIndex1 = vIndex1 - i;
                int zIndex2 = vIndex2 - j;

                //檢查地圖範圍
                if (CheckIsOverMapRange(zIndex1, zIndex2) == true)
                    return false;

                RMapBlock vBlock = mHome.Scene.Maps[zIndex1, zIndex2];

                if (vBlock.IsUsed == true)
                    return false;
            }
        }

        return true;
    }

    //檢查是否有空間(方向4)
    private bool CheckHaveSpace4 (int vIndex1, int vIndex2, RBottomBlock[,] vArea)
    {
        for (int j = 0; j < 3; j++)
        {
            for (int i = 0; i < 3; i++)
            {
                if (vArea[i, j].Value == 0)
                    continue;

                int zIndex1 = vIndex1 - j;
                int zIndex2 = vIndex2 + i;

                //檢查地圖範圍
                if (CheckIsOverMapRange(zIndex1, zIndex2) == true)
                    return false;

                RMapBlock vBlock = mHome.Scene.Maps[zIndex1, zIndex2];

                if (vBlock.IsUsed == true)
                    return false;
            }
        }

        return true;
    }

    //檢查是否有空間
    private bool CheckHaveSpace (int vIndex1, int vIndex2, RBottomBlock[,] vArea)
    {
        switch (mToward)
        {
            case 0: return CheckHaveSpace1(vIndex1, vIndex2, vArea);
            case 1: return CheckHaveSpace2(vIndex1, vIndex2, vArea);
            case 2: return CheckHaveSpace3(vIndex1, vIndex2, vArea);
            case 3: return CheckHaveSpace4(vIndex1, vIndex2, vArea);
            default: return false;
        }
    }

    //取得區塊陣列所引1
    private int GetBlockIndex1 (string vBlockName)
    {
        if ((vBlockName == null) || (vBlockName == ""))
            return -1;

        string[] vStr = vBlockName.Split(',');

        return vStr[0].ToInt();
    }

    //取得區塊陣列所引2
    private int GetBlockIndex2 (string vBlockName)
    {
        if ((vBlockName == null) || (vBlockName == ""))
            return -1;

        string[] vStr = vBlockName.Split(',');

        return vStr[1].ToInt();
    }

    //設置主介面物件
    private void SetHome (OUIFM_Home vHome)
    {
        mHome = vHome;
    }

    //設置錨點
    private void SetAnchor (Transform vAnchor)
    {
        //檢查錨點
        if (mAnchor == vAnchor)
            return;

        //檢查錨點是否為區塊
        if (vAnchor.tag == "Block")
            mAnchor = vAnchor;
        else
            mAnchor = null;

        //設置區塊顏色
        SetBlockColor();
    }

    //設置Panel
    private void SetPanel (Transform vPanel)
    {
        mPanel = vPanel;

        //檢查主介面物件
        if (mHome != null)
        {
            //掛入介面節點
            mHome.AddToBone(mPanel.gameObject, "Panel_Home");

            //設置按鈕事件
            mHome.OnUIClick(mPanel.Find("Button_Rotation").gameObject, Button_Rotation);
            mHome.OnUIClick(mPanel.Find("Button_OK").gameObject, Button_OK);
            mHome.OnUIClick(mPanel.Find("Button_Del").gameObject, Button_Del);
        }
    }

    //設置編號
    private void SetID (byte vID)
    {
        mID = vID;
    }

    //設置朝向
    private void SetToward (byte vToward)
    {
        mToward = (byte)(vToward % 4);

        switch (mToward)
        {
            case 0:
                transform.localEulerAngles += new Vector3(0, 0, 0);
                break;
            case 1:
                transform.localEulerAngles += new Vector3(0, 90, 0);
                break;
            case 2:
                transform.localEulerAngles += new Vector3(0, 180, 0);
                break;
            case 3:
                transform.localEulerAngles += new Vector3(0, 270, 0);
                break;
        }
    }

    //設置面積
    private void SetArea (eArea vArea)
    {
        mArea = vArea;

        //檢查面積資料
        if (mAreas == null)
        {
            //底部渲染索引
            int vIndex = 0;

            //建立底部資料
            mAreas = new RBottomBlock[3, 3];

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    //檢查面積值
                    if (Const_Home.Area[(byte)mArea, i, j] == 0)
                    {
                        mAreas[i, j] = new RBottomBlock(i, j, Const_Home.Area[(byte)mArea, i, j], null);
                    }
                    else
                    {
                        mAreas[i, j] = new RBottomBlock(i, j, Const_Home.Area[(byte)mArea, i, j], mBottomRenderer[vIndex]);

                        vIndex++;
                    }
                }
            }            
        }
    }

    //設置地圖值
    private void SetMapValue (bool vIsUsed)
    {
        if (mAnchor == null)
            return;

        int vIndex1 = GetBlockIndex1(mAnchor.name);
        int vIndex2 = GetBlockIndex2(mAnchor.name);

        mHome.Scene.SetMapValue(mID, mToward, vIndex1, vIndex2, vIsUsed);
    }

    //設置區塊顏色(方向1)
    private void SetBlockColor1 (int vIndex1, int vIndex2)
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (mAreas[i, j].Renderer == null)
                    continue;

                int zIndex1 = vIndex1 + i;
                int zIndex2 = vIndex2 + j;

                //檢查地圖範圍
                if (CheckIsOverMapRange(zIndex1, zIndex2) == true)
                {
                    mAreas[i, j].Renderer.material.color = Color.red;
                    continue;
                }

                RMapBlock vBlock = mHome.Scene.Maps[zIndex1, zIndex2];

                if (vBlock.IsUsed == true)
                {
                    mAreas[i, j].Renderer.material.color = Color.red;
                }
                else
                {
                    mAreas[i, j].Renderer.material.color = Color.blue;
                }
            }
        }
    }

    //設置區塊顏色(方向2)
    private void SetBlockColor2 (int vIndex1, int vIndex2)
    {
        for (int j = 0; j < 3; j++)
        {
            for (int i = 0; i < 3; i++)
            {
                if (mAreas[i, j].Renderer == null)
                    continue;

                int zIndex1 = vIndex1 + j;
                int zIndex2 = vIndex2 - i;

                //檢查地圖範圍
                if (CheckIsOverMapRange(zIndex1, zIndex2) == true)
                {
                    mAreas[i, j].Renderer.material.color = Color.red;
                    continue;
                }

                RMapBlock vBlock = mHome.Scene.Maps[zIndex1, zIndex2];

                if (vBlock.IsUsed == true)
                {
                    mAreas[i, j].Renderer.material.color = Color.red;
                }
                else
                {
                    mAreas[i, j].Renderer.material.color = Color.blue;
                }
            }
        }
    }

    //設置區塊顏色(方向3)
    private void SetBlockColor3 (int vIndex1, int vIndex2)
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (mAreas[i, j].Renderer == null)
                    continue;

                int zIndex1 = vIndex1 - i;
                int zIndex2 = vIndex2 - j;

                //檢查地圖範圍
                if (CheckIsOverMapRange(zIndex1, zIndex2) == true)
                {
                    mAreas[i, j].Renderer.material.color = Color.red;
                    continue;
                }

                RMapBlock vBlock = mHome.Scene.Maps[zIndex1, zIndex2];

                if (vBlock.IsUsed == true)
                {
                    mAreas[i, j].Renderer.material.color = Color.red;
                }
                else
                {
                    mAreas[i, j].Renderer.material.color = Color.blue;
                }
            }
        }
    }

    //設置區塊顏色(方向4)
    private void SetBlockColor4 (int vIndex1, int vIndex2)
    {
        for (int j = 0; j < 3; j++)
        {
            for (int i = 0; i < 3; i++)
            {
                if (mAreas[i, j].Renderer == null)
                    continue;

                int zIndex1 = vIndex1 - j;
                int zIndex2 = vIndex2 + i;

                //檢查地圖範圍
                if (CheckIsOverMapRange(zIndex1, zIndex2) == true)
                {
                    mAreas[i, j].Renderer.material.color = Color.red;
                    continue;
                }

                RMapBlock vBlock = mHome.Scene.Maps[zIndex1, zIndex2];

                if (vBlock.IsUsed == true)
                {
                    mAreas[i, j].Renderer.material.color = Color.red;
                }
                else
                {
                    mAreas[i, j].Renderer.material.color = Color.blue;
                }
            }
        }
    }

    //設置區塊顏色
    private void SetBlockColor ()
    {
        if (mIsEdit == false)
            return;

        if (mAreas == null)
            return;

        if (mAnchor == null)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (mAreas[i, j].Renderer == null)
                        continue;

                    mAreas[i, j].Renderer.material.color = Color.red;
                }
            }
        }
        else
        {
            int vIndex1 = GetBlockIndex1(mAnchor.name);
            int vIndex2 = GetBlockIndex2(mAnchor.name);

            switch (mToward)
            {
                case 0:
                    SetBlockColor1(vIndex1, vIndex2);
                    break;
                case 1:
                    SetBlockColor2(vIndex1, vIndex2);
                    break;
                case 2:
                    SetBlockColor3(vIndex1, vIndex2);
                    break;
                case 3:
                    SetBlockColor4(vIndex1, vIndex2);
                    break;
            }
        }
    }

    //設置地圖碰撞盒
    private void SetMapCollider (bool vIsEnable)
    {
        if (mHome.Scene.List.IndexOf(this) == -1)
            mHome.Scene.List.Add(this);

        for (int i = 0; i < mHome.Scene.List.Count; i++)
        {
            if (mHome.Scene.List[i] == this)
                continue;

            mHome.Scene.List[i].Collider.enabled = vIsEnable;
        }
    }

    //校正位置
    public void FixPosition ()
    {
        if (mAnchor == null)
            return;

        transform.position = new Vector3(mAnchor.position.x, transform.position.y, mAnchor.position.z);
    }

    //升起建築
    public void BuildingRaiseUp ()
    {
        //檢查是否可以升起建築物
        if (CheckCanBuildingRaiseUp() == false)
            return;

        //設置地圖值
        SetMapValue(false);

        //設置地圖碰撞盒
        SetMapCollider(false);

        //設置目標建築物
        mHome.Building = this;

        //設置編輯標記
        mIsEdit = true;

        for (int i = 0; i < mBuildingRenderer.Length; i++)
            mBuildingRenderer[i].sortingOrder = 2;

        for (int i = 0; i < mBottomRenderer.Length; i++)
            mBottomRenderer[i].sortingOrder = 1;

        mBuilding.transform.localPosition = new Vector3(0, 0.6f, 0);
        mBottom.transform.localPosition = new Vector3(0, 0.1f, 0);

        //顯示底部區塊
        mBottom.gameObject.SetActive(true);

        //顯示介面
        if (mPanel != null)
            mPanel.gameObject.SetActive(true);

        //設置介面位置
        mHome.Panel_Main.transform.localPosition = new Vector3(0, 960, 0);
        mHome.Panel_Storehouse.transform.localPosition = new Vector3(0, 960, 0);
        mHome.Panel_Edit.transform.localPosition = new Vector3(0, 0, 0);

        //顯示地塊
        mHome.Scene.ShowBlock();

        OUIFM_Chat vChat = (OUIFM_Chat)GameInfo.UIMgr.GetUI(eUI.Chat);

        if (vChat != null)
            vChat.Hide();
    }

    //放下建築
    public void BuildingLayDown ()
    {
        //檢查是否可以放下建築物
        if (CheckCanBuildingLayDown() == false)
            return;

        //設置地圖值
        SetMapValue(true);

        //設置地圖碰撞盒
        SetMapCollider(true);

        //設置目標建築物
        mHome.Building = null;

        //設置編輯標記
        mIsEdit = false;

        for (int i = 0; i < mBuildingRenderer.Length; i++)
            mBuildingRenderer[i].sortingOrder = 0;

        for (int i = 0; i < mBottomRenderer.Length; i++)
            mBottomRenderer[i].sortingOrder = 0;
        
        mBuilding.transform.localPosition = new Vector3(0, 0, 0);
        mBottom.transform.localPosition = new Vector3(0, 0, 0);

        //隱藏底部區塊
        mBottom.gameObject.SetActive(false);

        //隱藏介面
        if (mPanel != null)
            mPanel.gameObject.SetActive(false);

        //設置介面位置
        mHome.Panel_Main.transform.localPosition = new Vector3(0, 0, 0);
        mHome.Panel_Storehouse.transform.localPosition = new Vector3(0, 960, 0);
        mHome.Panel_Edit.transform.localPosition = new Vector3(0, 960, 0);

        //隱藏地塊
        mHome.Scene.HideBlock();

        OUIFM_Chat vChat = (OUIFM_Chat)GameInfo.UIMgr.GetUI(eUI.Chat);

        if (vChat != null)
            vChat.Show();
    }

    //旋轉建築
    public void BuildingRotation ()
    {
        //檢查是否可以旋轉建築
        if (CheckCanBuildingRotation() == false)
            return;

        mToward++;

        mToward = (byte)(mToward % 4);

        transform.localEulerAngles += new Vector3(0, 90, 0);

        SetBlockColor();
    }

    //事件 旋轉按鈕
    private void Button_Rotation (object vObject)
    {
        BuildingRotation();
    }

    //事件 確認按鈕
    private void Button_OK (object vObject)
    {
        if (GameInfo.RoleMgr.IsLogin == false)
        {
            BuildingLayDown();
        }
        else
        {
            Send_BuildingLayDown();
        }
    }

    //事件 刪除按鈕
    private void Button_Del (object vObject)
    {
        //設置介面位置
        mHome.Panel_Main.transform.localPosition = new Vector3(0, 0, 0);
        mHome.Panel_Storehouse.transform.localPosition = new Vector3(0, 960, 0);
        mHome.Panel_Edit.transform.localPosition = new Vector3(0, 960, 0);

        //隱藏地塊
        mHome.Scene.HideBlock();

        OUIFM_Chat vChat = (OUIFM_Chat)GameInfo.UIMgr.GetUI(eUI.Chat);

        if (vChat != null)
            vChat.Show();

        //設置地圖碰撞盒
        SetMapCollider(true);

        //設置目標建築物
        mHome.Building = null;

        //取得建築列表索引值
        int vIndex = mHome.Scene.List.IndexOf(this);

        //檢查索引值
        if (vIndex != -1)
            mHome.Scene.List.RemoveAt(vIndex);

        //刪除建築介面
        Destroy(mPanel.gameObject);

        //刪除建築物
        Destroy(gameObject);
    }

    //協定
    public void Send_BuildingRaiseUp ()
    {
        if (GameInfo.RoleMgr.IsLogin == false)
            return;

        if (mAnchor == null)
            return;

        int vIndex1 = GetBlockIndex1(mAnchor.name) + 1;
        int vIndex2 = GetBlockIndex2(mAnchor.name) + 1;

        ByteArrayBuffer vMsg = new ByteArrayBuffer();
        vMsg.WriteInt(Hash);
        vMsg.WriteByte(mID);
        vMsg.WriteByte((byte)vIndex1);
        vMsg.WriteByte((byte)vIndex2);
        vMsg.WriteByte(mToward);
        GameInfo.NetMgr.SendMessage(Const_Net.PRO_HOME, 003, vMsg);
    }

    //協定
    public void Send_BuildingLayDown ()
    {
        if (GameInfo.RoleMgr.IsLogin == false)
            return;

        if (mAnchor == null)
            return;

        int vIndex1 = GetBlockIndex1(mAnchor.name) + 1;
        int vIndex2 = GetBlockIndex2(mAnchor.name) + 1;

        ByteArrayBuffer vMsg = new ByteArrayBuffer();
        vMsg.WriteInt(Hash);
        vMsg.WriteByte(mID);
        vMsg.WriteByte((byte)vIndex1);
        vMsg.WriteByte((byte)vIndex2);
        vMsg.WriteByte(mToward);
        GameInfo.NetMgr.SendMessage(Const_Net.PRO_HOME, 004, vMsg);
    }
}
