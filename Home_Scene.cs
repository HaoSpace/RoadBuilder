using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Home_Scene : MonoBehaviour
{
    private OUIFM_Home mHome = null;           //主介面
    private OHome_Building mBuilding = null;   //建築物
    private Transform mTL = null;              //定位點
    private Transform mBR = null;              //定位點
	private Transform mRoad = null;            //路面
	private GameObject[] mExtendPoint = null;  //路面延伸點擊
    private Renderer[] mBlockRenderers = null; //地塊渲染表
	private Material[] mRoadMaterial = null;   //區塊事件貼圖材質
    private bool mIsPress = false;             //按壓標記
	private bool mIsDraw = false;              //畫圖標記
	private bool mIsRoadEdit = false;          //道路編輯標記
	private bool mIsRoadMove = false;          //道路移動標記
    private long mTime = 0;                    //時間標記
	private List<RMapBlock> mRoadList = null;  //道路列表
    private List<OHome_Building> mList = null; //建築物列表
	private List<RSimpleBlock> mSimpleList = null;

    private RMapBlock[,] mMaps = null;         //地圖表

    public OUIFM_Home Home {get{return mHome;} set{SetHome(value);}}
    public OHome_Building Building {get{return mBuilding;}}
    public Transform TL {get{return mTL;}}
    public Transform BR {get{return mBR;}}
    public Renderer[] BlockRenderers {get{return mBlockRenderers;}}
    public List<OHome_Building> List {get{return mList;}}
    public RMapBlock[,] Maps {get{return mMaps;}}

    void Awake ()
    {
        //建立建築物列表
        mList = new List<OHome_Building>();

        mTL = transform.Find("Anchor/TL");

        mBR = transform.Find("Anchor/BR");

        //初始化地塊
        Init_Block();

		//初始化延伸點
		Init_ExtendPoint();
    }

	// Update is called once per frame
    void Update ()
    {
        //更新按壓狀態
        Update_Press();

		//更新路面規劃
		Update_DrawRoad ();

        //更新鎖定建築物
        Update_Building();

        //更新建築物移動
        Update_BuildingMove();

        //更新建築編輯狀態
        Update_BuildingEdit();

		//更新路面管理UI的位置
		Updata_RoadEditor();

		//更新道路移動
		Update_RoadMove();
    }

    //更新按壓狀態
    private void Update_Press ()
    {
        #if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            mIsPress = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            mIsPress = false;

			//移動道路事件
			if (mIsRoadMove == true)
			{
				mIsRoadMove = false;
			}
				
			//繪製道路事件
			if (mIsDraw == true)
			{
				Road_DrawEnd();
			}
				
			//建築物事件
            if (mBuilding != null)
            {
                mBuilding.FixPosition();
                mBuilding = null;
            }
        }
        else if (Input.GetMouseButton(0))
        {

        }
        #else
        if ((Input.touchCount > 0) && (Input.GetTouch(0).phase == TouchPhase.Began))
        {
            mIsPress = true;
        }
        else if ((Input.touchCount > 0) && (Input.GetTouch(0).phase == TouchPhase.Ended))
        {
            mIsPress = false;

			//移動道路事件
			if (mIsRoadMove == true)
			{
				mIsRoadMove = false;
			}
			
			//繪製道路事件
			if (mIsDraw == true)
			{
				Road_DrawEnd();
			}
			
            if (mBuilding != null)
            {
                mBuilding.FixPosition();
                mBuilding = null;
            }
        }
        else if ((Input.touchCount > 0) && (Input.GetTouch(0).phase == TouchPhase.Canceled))
        {
            mIsPress = false;

			//移動道路事件
			if (mIsRoadMove == true)
			{
				mIsRoadMove = false;
			}
			
			//繪製道路事件
			if (mIsDraw == true)
			{
				Road_DrawEnd();
			}
			
            if (mBuilding != null)
            {
                mBuilding.FixPosition();
                mBuilding = null;
            }
        }
        else if ((Input.touchCount > 0) && (Input.GetTouch(0).phase == TouchPhase.Moved))
        {

        }
        #endif
    }

	private void Update_DrawRoad ()
	{
		//檢查按壓標記
		if (mIsPress == false)
			return;

		Road_Draw();
	}

	//更新鎖定建築物
    private void Update_Building ()
    {
        //檢查按壓標記
        if (mIsPress == false)
            return;

        //檢查鎖定建築物
        if (mBuilding == null)
        {
            Ray vRay = mHome.Camera.Camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit vHit;
            int vLayerMask = 1 << LayerMask.NameToLayer("Building");

            //檢查是否有偵測到建築物
            if (Physics.Raycast(mHome.Camera.transform.position, vRay.direction, out vHit, Mathf.Infinity, vLayerMask))
            {
				//畫道路時避開
				if(mIsDraw == true)
					return;

                //更新時間
                mTime = System.DateTime.Now.Ticks;

                //取得建築物腳本
                mBuilding = vHit.collider.transform.GetComponent<OHome_Building>();
            }
        }
    }

    //更新建築物移動
    private void Update_BuildingMove ()
    {
        //檢查按壓標記
        if (mIsPress == false)
            return;

        if (UICamera.isOverUI == true)
            return;

        Ray vRay = mHome.Camera.Camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit vHit;
        int vLayerMask = ((1 << LayerMask.NameToLayer("Ground")) | (1 << LayerMask.NameToLayer("Block")));

        //檢查是否有偵測到地塊
        if (Physics.Raycast(mHome.Camera.transform.position, vRay.direction, out vHit, Mathf.Infinity, vLayerMask))
        {
            #if Debug
            //計算射線長度
            float vDistance = Vector3.Distance(mHome.Camera.transform.position, vHit.point);

            //繪製射線
            Debug.DrawRay(mHome.Camera.transform.position, vRay.direction * vDistance, Color.green);
            #endif

            //檢查鎖定建築是否相符
            if ((mHome.Building != null) && (mBuilding != null) && (mHome.Building == mBuilding))
            {
                //取得錨點
                mBuilding.Anchor = vHit.collider.transform;

                //更新位置
                mBuilding.transform.position = new Vector3(vHit.point.x, 0.01f, vHit.point.z);
            }
        }
    }

    //更新建築編輯狀態
    private void Update_BuildingEdit ()
    {
        //檢查按壓標記
        if (mIsPress == false)
            return;
        
        //檢查是否已有編輯中建築物
        if (mHome.Building != null)
            return;

        //檢查是否有鎖定建築物
        if (mBuilding == null)
            return;

        //取得當前時間
        long vTime = System.DateTime.Now.Ticks;

        //檢查時間
        if ((vTime - mTime) < 10000000)
            return;

        if (GameInfo.RoleMgr.IsLogin == false)
        {
            mBuilding.BuildingRaiseUp();
        }
        else
        {
            mBuilding.Send_BuildingRaiseUp();
        }

        //更新時間
        mTime = System.DateTime.Now.Ticks;
    }

	//更新道路管理 位置
	private void Updata_RoadEditor ()
	{
		if (mIsRoadEdit == false)
			return;

		Road_ShowGroup();
	}

	private void Update_RoadMove ()
	{
		//檢查按壓標記
		if (mIsPress == false)
			return;

		Road_Move();
	}

	//判斷區塊是否有使用
	private bool CheckBlockUsed (Transform vTrans)
	{
		int vIndex1 = GetBlockIndex(vTrans, 1);
		int vIndex2 = GetBlockIndex(vTrans, 2);
		
		if (vIndex1 > -1 && vIndex1 > -1)
			return mMaps[vIndex1, vIndex2].IsUsed;

		return true;
	}

	private byte CheckBlockIsRoad (RSimpleBlock vBlock, byte vRoadNum)
	{
		if (vBlock.Row > -1 && vBlock.Col > -1)
		{
			if(mMaps[vBlock.Row, vBlock.Col].IsRoad == true)
				return vRoadNum;
		}

		return 0;
	}

	//判斷路 連結
	private bool CheckRoadConnect (Transform vTrans)
	{
		//起始點路面
		if (mRoad == null)
		{
			mRoad = vTrans;
			return true;
		}

		int vStartX = GetBlockIndex(mRoad, 1);
		int vStartY = GetBlockIndex(mRoad, 2);
		int vEndX = GetBlockIndex(vTrans, 1);
		int vEndY = GetBlockIndex(vTrans, 2);

		if (vStartX == vEndX)
		{
			if ((vStartY == vEndY + 1) || (vStartY == vEndY - 1))
				return true;
		}
		else if (vStartY == vEndY)
		{
			if ((vStartX == vEndX + 1) || (vStartX == vEndX - 1)) 
				return true;
		}

		return false;
	}

	//撿查 兩點在 相同水平/垂直 上
	//1.水平 2.垂直 3.沒有
	private byte CheckBlockInLine (Transform vTrans)
	{
		int vStartX = GetBlockIndex(mRoad, 1);
		int vStartY = GetBlockIndex(mRoad, 2);
		int vEndX = GetBlockIndex(vTrans, 1);
		int vEndY = GetBlockIndex(vTrans, 2);

		if (vStartX == vEndX) return 1;
		if (vStartY == vEndY) return 2;

		return 3;
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

	private bool CheckRoadMove (Transform vTrans)
	{
		if(mRoad != vTrans)
			return true;

		return false;
	}



	//取得區塊 1.X值 2.Y值
	private int GetBlockIndex (Transform vTrans, byte vIndex)
	{
		if (vTrans == null)
			return -1;

		if (vIndex < 1 || vIndex > 2)
			return -1;

		string[] vStr = vTrans.name.Split(',');

		if (vStr.Length != 2)
			return -1;
		
		return vStr[vIndex - 1].ToInt();
	}

	//取得道路鄰近連結
	private RSimpleBlock[] GetRoadConnection (RSimpleBlock vBlock, out byte vCheckNo)
	{
		vCheckNo = 0;
		byte vTmpNo = vCheckNo;
		List<RSimpleBlock> vList = new List<RSimpleBlock>();

		vCheckNo += CheckBlockIsRoad(new RSimpleBlock(vBlock.Row, vBlock.Col + 1), 4);

		if (vTmpNo < vCheckNo)
		{
			vTmpNo = vCheckNo;
			vList.Add(new RSimpleBlock(vBlock.Row, vBlock.Col + 1));
		}

		vCheckNo += CheckBlockIsRoad(new RSimpleBlock(vBlock.Row + 1, vBlock.Col), 8);

		if (vTmpNo < vCheckNo)
		{
			vTmpNo = vCheckNo;
			vList.Add(new RSimpleBlock(vBlock.Row + 1, vBlock.Col));
		}

		vCheckNo += CheckBlockIsRoad(new RSimpleBlock(vBlock.Row, vBlock.Col - 1), 1);

		if (vTmpNo < vCheckNo)
		{
			vTmpNo = vCheckNo;
			vList.Add(new RSimpleBlock(vBlock.Row, vBlock.Col - 1));
		}

		vCheckNo += CheckBlockIsRoad(new RSimpleBlock(vBlock.Row - 1, vBlock.Col), 2);

		if (vTmpNo < vCheckNo)
		{
			vTmpNo = vCheckNo;
			vList.Add(new RSimpleBlock(vBlock.Row - 1, vBlock.Col));
		}

		return vList.ToArray();
	}

	private byte GetRoadConnection (RSimpleBlock vBlock)
	{
		byte vCheckNo = 0;

		vCheckNo += CheckBlockIsRoad(new RSimpleBlock(vBlock.Row, vBlock.Col + 1), 4);
		vCheckNo += CheckBlockIsRoad(new RSimpleBlock(vBlock.Row + 1, vBlock.Col), 8);
		vCheckNo += CheckBlockIsRoad(new RSimpleBlock(vBlock.Row, vBlock.Col - 1), 1);
		vCheckNo += CheckBlockIsRoad(new RSimpleBlock(vBlock.Row - 1, vBlock.Col), 2);

		return vCheckNo;
	}

    //取得建築物
    private IEnumerator GetBuilding (RHomeBuildingJson vBuildingData, TObject vObj)
    {
        //尋找同類型建築
        for (int i = 0; i < mList.Count; i++)
        {
            if (mList[i].ID != vBuildingData.ID)
                continue;

            vObj.Obj = GameObject.Instantiate(mList[i].gameObject);
            yield break;
        }

        //場上不存在同類型建築，載入建築物
        string vPath = Application.persistentDataPath + "/" + GameInfo.LangPath + Const_Common.Path_Home + Const_Common.Path_Models_Items;

        yield return GameInfo.CoroutineMgr.StartCoroutine(GameInfo.DataMgr.ILoadBundle(vPath, vBuildingData.BundleName, vObj));

        if (vObj.Err != null)
        {
            GameInfo.Log(vObj.Err);
        }
        else
        {
            vObj.Obj = GameObject.Instantiate(vObj.Bundle.assetBundle.mainAsset) as GameObject;
            vObj.Bundle.assetBundle.Unload(false);
        }
    }

	//取得道路移動量
	private RSimpleBlock GetMoveMeasure (Transform vTrans)
	{
		int vOriginX = GetBlockIndex(mRoad, 1);
		int vOriginY = GetBlockIndex(mRoad, 2);
		int vNowX = GetBlockIndex(vTrans, 1);
		int vNowY = GetBlockIndex(vTrans, 2);

		return new RSimpleBlock(vNowX - vOriginX, vNowY - vOriginY);
	}

	private void SetBlockUsed (Transform vTrans, bool vIsUsed)
	{	
		int vIndex1 = GetBlockIndex(vTrans, 1);
		int vIndex2 = GetBlockIndex(vTrans, 2);

		if (vIndex1 > -1 && vIndex1 > -1)
		{
			mMaps[vIndex1, vIndex2].IsUsed = vIsUsed;
			mMaps[vIndex1, vIndex2].IsRoad = vIsUsed;
		}
			
	}

	private void SetGroupBlockMat (RSimpleBlock[] vBlockGroup)
	{
		for (int i = 0; i < vBlockGroup.Length; i++)
		{
			byte vCheckNo = GetRoadConnection(vBlockGroup[i]);
			SetBlockMat(Maps[vBlockGroup[i].Row, vBlockGroup[i].Col].RenderMat, vCheckNo);
		}
	}

	//設置道路材質
	private void SetBlockMat (Renderer vRenderer, byte vCheckRoadNo)
	{
		SetMatInStore(vRenderer.material);

		int vIndex = GameInfo.DataMgr.Data_HomeRoad.GetMatIndex(vCheckRoadNo);

		vRenderer.material = mRoadMaterial[vIndex];
	}

	//紀錄原本道路的材質
	private void SetMatInStore (Material vMat)
	{
		return;
	}


	//使道路連結起來
	private void SetRoadConnect (Transform vTrans)
	{
		int vStart = 0;
		int vEnd = 0;
		int vIndexX = 0;
		int vIndexY = 0;
		int vBlockCount = 0;
		Transform vBlock = null;
		byte vCheckLine = CheckBlockInLine(vTrans);

		switch(vCheckLine)
		{
		case 1:
			vStart = GetBlockIndex(mRoad,2);
			vEnd = GetBlockIndex(vTrans,2);
			vIndexX = GetBlockIndex(vTrans, 1);

			if (vStart < vEnd)
				vIndexY = vStart;
			else
				vIndexY = vEnd;

			vBlockCount = Mathf.Abs(vStart - vEnd) - 1;

			for (int i = 1; i <= vBlockCount; i++)
			{
				vBlock = mMaps[vIndexX, vIndexY + i].Anchor;
				SetBlockUsed(vBlock, true);
				Road_AddList(vBlock, false);

			}

			break;
		case 2:
			vStart = GetBlockIndex(mRoad,1);
			vEnd = GetBlockIndex(vTrans,1);
			vIndexY = GetBlockIndex(vTrans, 2);

			if (vStart < vEnd)
				vIndexX = vStart;
			else
				vIndexX = vEnd;

			vBlockCount = Mathf.Abs(vStart - vEnd) - 1;
			
			for (int i = 1; i <= vBlockCount; i++)
			{
				vBlock = mMaps[vIndexX + i, vIndexY].Anchor;
				SetBlockUsed(vBlock, true);
				Road_AddList(vBlock, false);

			}

			break;
		case 3:
			vIndexX = GetBlockIndex(mRoad,1);
			vIndexY = GetBlockIndex(vTrans,2);
			vBlock = mMaps[vIndexX, vIndexY].Anchor;

			SetRoadConnect(vBlock);
			SetBlockUsed(vBlock, true);
			Road_AddList(vBlock, false);


			mRoad = vBlock;

			SetRoadConnect(vTrans);

			break;
		}
	}

	private void SetNextRoadMove (Transform vTrans)
	{
		RSimpleBlock vData = GetMoveMeasure(vTrans);

		//舊的座標復原
		for (int i = 0; i < mRoadList.Count; i++)
		{
			SetBlockUsed(mRoadList[i].Anchor, false);
		}

		List<RMapBlock> vList = new List<RMapBlock>();
		
		//取得新的作標位置
		for (int i = 0; i < mSimpleList.Count; i++)
		{
			int vNewPosX = mSimpleList[i].Row + vData.Row;
			int vNewPosY = mSimpleList[i].Col + vData.Col;
			
			if (!CheckIsOverMapRange(vNewPosX, vNewPosY))
			{
				RMapBlock vRoadData = Maps[vNewPosX, vNewPosY];

				vList.Add(vRoadData);
				SetBlockUsed(vRoadData.Anchor, true);
			}
		}

		if(vList.Count != 0)
		{
			mRoadList = vList;
		}
	}

    //設置主介面
    private void SetHome (OUIFM_Home vHome)
    {
        mHome = vHome;
    }

    //設置地圖值
    public void SetMapValue (byte vBuildingID, int vToward, int vIndex1, int vIndex2, bool vIsUsed)
    {
        switch (vToward)
        {
            case 0:
                SetMapValue1(vBuildingID, vIndex1, vIndex2, vIsUsed);
                break;
            case 1:
                SetMapValue2(vBuildingID, vIndex1, vIndex2, vIsUsed);
                break;
            case 2:
                SetMapValue3(vBuildingID, vIndex1, vIndex2, vIsUsed);
                break;
            case 3:
                SetMapValue4(vBuildingID, vIndex1, vIndex2, vIsUsed);
                break;
        }
    }

    //設置地圖值(方向1)
    private void SetMapValue1 (byte vBuildingID, int vIndex1, int vIndex2, bool vIsUsed)
    {
        RHomeBuildingJson vData = GameInfo.DataMgr.Data_HomeBuilding.GetDataByKey(vBuildingID);

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (Const_Home.Area[vData.Area, i, j] == 0)
                    continue;

                int zIndex1 = vIndex1 + i;
                int zIndex2 = vIndex2 + j;

                //檢查地圖範圍
                if (CheckIsOverMapRange(zIndex1, zIndex2) == true)
                    continue;
                
                mMaps[zIndex1, zIndex2].IsUsed = vIsUsed;
            }
        }
    }

    //設置地圖值(方向2)
    private void SetMapValue2 (byte vBuildingID, int vIndex1, int vIndex2, bool vIsUsed)
    {
        RHomeBuildingJson vData = GameInfo.DataMgr.Data_HomeBuilding.GetDataByKey(vBuildingID);

        for (int j = 0; j < 3; j++)
        {
            for (int i = 0; i < 3; i++)
            {
                if (Const_Home.Area[vData.Area, i, j] == 0)
                    continue;

                int zIndex1 = vIndex1 + j;
                int zIndex2 = vIndex2 - i;

                //檢查地圖範圍
                if (CheckIsOverMapRange(zIndex1, zIndex2) == true)
                    continue;
                
                mMaps[zIndex1, zIndex2].IsUsed = vIsUsed;
            }
        }
    }

    //設置地圖值(方向3)
    private void SetMapValue3 (byte vBuildingID, int vIndex1, int vIndex2, bool vIsUsed)
    {
        RHomeBuildingJson vData = GameInfo.DataMgr.Data_HomeBuilding.GetDataByKey(vBuildingID);

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (Const_Home.Area[vData.Area, i, j] == 0)
                    continue;

                int zIndex1 = vIndex1 - i;
                int zIndex2 = vIndex2 - j;

                //檢查地圖範圍
                if (CheckIsOverMapRange(zIndex1, zIndex2) == true)
                    continue;

                mMaps[zIndex1, zIndex2].IsUsed = vIsUsed;
            }
        }
    }

    //設置地圖值(方向4)
    private void SetMapValue4 (byte vBuildingID, int vIndex1, int vIndex2, bool vIsUsed)
    {
        RHomeBuildingJson vData = GameInfo.DataMgr.Data_HomeBuilding.GetDataByKey(vBuildingID);

        for (int j = 0; j < 3; j++)
        {
            for (int i = 0; i < 3; i++)
            {
                if (Const_Home.Area[vData.Area, i, j] == 0)
                    continue;

                int zIndex1 = vIndex1 - j;
                int zIndex2 = vIndex2 + i;

                //檢查地圖範圍
                if (CheckIsOverMapRange(zIndex1, zIndex2) == true)
                    continue;

                mMaps[zIndex1, zIndex2].IsUsed = vIsUsed;
            }
        }
    }

    //初始化地塊
    private void Init_Block ()
    {
        //取得地塊渲染
        mBlockRenderers = transform.FindChild("Block").GetComponentsInChildren<Renderer>();

        //重新排列地塊位置
        for (int i = 0; i < mBlockRenderers.Length; i++)
        {
            int vModX = (i + 1) % Const_Home.MAX_COL;

            if (vModX == 0)
                vModX = Const_Home.MAX_COL;

            int vModY = i / Const_Home.MAX_COL;

            float vX = -26.5f + vModX;
            float vY = vModY - 17.51f;

            mBlockRenderers[i].transform.localPosition = new Vector3(vY, -0.051f, vX);
        }

        //地塊索引
        int vIndex = 0;

        //建立地圖資料表
        mMaps = new RMapBlock[Const_Home.MAX_ROW, Const_Home.MAX_COL];

        for (int i = 0; i < mMaps.GetLength(0); i++)
        {
            for (int j = 0; j < mMaps.GetLength(1); j++)
            {
                //重新命名地塊名稱
                mBlockRenderers[vIndex].name = string.Format("{0},{1}", i, j);

                //建立地塊資料
				mMaps[i, j] = new RMapBlock(i, j, mBlockRenderers[vIndex].transform, mBlockRenderers[vIndex], false);

                //遞增索引值
                vIndex++;
            }
        }

        //隱藏地塊
        HideBlock();
    }

	private void Init_ExtendPoint ()
	{
		GameInfo.CoroutineMgr.StartCoroutine(Build_ExtendPoint());
	}

	private IEnumerator Build_ExtendPoint ()
	{
		mExtendPoint = new GameObject[2];

		//取得建築物資料
		RHomeBuildingJson vStartData = GameInfo.DataMgr.Data_HomeBuilding.GetDataByKey(4);
		RHomeBuildingJson vEndData = GameInfo.DataMgr.Data_HomeBuilding.GetDataByKey(6);
		
		TObject vObj = new TObject();
		TObject zObj = new TObject();
		
		//取得建築物
		yield return GameInfo.CoroutineMgr.StartCoroutine(GetBuilding(vStartData, vObj));
		yield return GameInfo.CoroutineMgr.StartCoroutine(GetBuilding(vEndData, zObj));
		
		//檢查是否有取得
		if (vObj.Obj == null || zObj.Obj == null)
		{
			yield break;
		}
		else
		{
			//設置建築物位置.角度.大小
			mExtendPoint[0] = vObj.Obj;
			mExtendPoint[0].transform.parent = transform;
			mExtendPoint[0].transform.position = new Vector3(0, 960, 0);
			mExtendPoint[0].transform.localEulerAngles = new Vector3(0, 0, 0);
			mExtendPoint[0].transform.localScale = Vector3.one / transform.localScale.x;

			//設置建築物位置.角度.大小
			mExtendPoint[1] = zObj.Obj;
			mExtendPoint[1].transform.parent = transform;
			mExtendPoint[1].transform.position = new Vector3(0, 960, 0);
			mExtendPoint[1].transform.localEulerAngles = new Vector3(0, 0, 0);
			mExtendPoint[1].transform.localScale = Vector3.one / transform.localScale.x;
		}
	}

	//建立道路材質
	public IEnumerator Build_RoadMaterial ()
	{
		TObject vObj = new TObject();

		string vPath = Application.persistentDataPath + "/" + GameInfo.LangPath + Const_Common.Path_Home + Const_Common.Path_Mats;

		yield return GameInfo.CoroutineMgr.StartCoroutine(GameInfo.DataMgr.ILoadBundle(vPath, "Mat_Road", vObj));

		if (vObj.Err != null)
		{
			GameInfo.Log(vObj.Err);
		}
		else
		{
			GameObject vRoadMat = GameObject.Instantiate(vObj.Bundle.assetBundle.mainAsset) as GameObject;
			vRoadMat.name = "Mat_Road";
			
			Renderer vRenderer = vRoadMat.GetComponent<Renderer>();
			
			if (vRenderer != null)
			{
				List<Material> vList = new List<Material>();

				for (int i = 1; i <= vRenderer.materials.Length; i++)
				{
					string vMatName = GameInfo.DataMgr.Data_HomeRoad.GetMatName((ushort)i);
					vMatName = vMatName + " (Instance)";

					if(vMatName == "")
						continue;

					for (int k = 0; k <  vRenderer.materials.Length; k++)
					{
						if (vRenderer.materials[k].name != vMatName)
							continue;

						vList.Add(vRenderer.materials[k]);
					}
				}

				mRoadMaterial = vList.ToArray();
			}
			
			mHome.AddToBone(vRoadMat, "Model/Scene_Home");
			
			vObj.Bundle.assetBundle.Unload(false);
		}
		
		vObj.Dispose();
		vObj = null;


	}

    //建立建築物(用在進場景時)
    public IEnumerator Build_Building (RBuildingData vData)
    {
        //取得建築物資料
        RHomeBuildingJson vBuildingData = GameInfo.DataMgr.Data_HomeBuilding.GetDataByKey(vData.ID);

        TObject vObj = new TObject();

        //取得建築物
        yield return GameInfo.CoroutineMgr.StartCoroutine(GetBuilding(vBuildingData, vObj));

        //檢查是否有取得
        if (vObj.Obj == null)
        {
            yield break;
        }
        else
        {
            GameObject vBuilding = vObj.Obj;
            int vIndex1 = vData.Index1 - 1;
            int vIndex2 = vData.Index2 - 1;

            //取得建築物所在位置
            Vector3 vPos = new Vector3(mMaps[vIndex1, vIndex2].Anchor.position.x, 0.01f, mMaps[vIndex1, vIndex2].Anchor.position.z);;

            //設置建築物位置.角度.大小
            vBuilding.transform.parent = transform;
            vBuilding.transform.position = vPos;
            vBuilding.transform.localEulerAngles = Vector3.zero;
            vBuilding.transform.localScale = Vector3.one;

            //掛載建築物腳本
            OHome_Building xBuilding = vBuilding.GetComponent<OHome_Building>();

            if (xBuilding == null)
                xBuilding = vBuilding.AddComponent<OHome_Building>();

            xBuilding.Home = mHome;
            xBuilding.Anchor = mMaps[vIndex1, vIndex2].Anchor;
            xBuilding.Panel = GameObject.Instantiate(mHome.Panel_Building).transform;
            xBuilding.ID = vData.ID;
            xBuilding.Toward = vData.Toward;
            xBuilding.Area = (eArea)vBuildingData.Area;

            //加入列表
            if (mList.IndexOf(xBuilding) == -1)
            {
                mList.Add(xBuilding);

                SetMapValue(vData.ID, vData.Toward, vIndex1, vIndex2, true);
            }
        }

        vObj.Dispose();
        vObj = null;
    }

    //建立建築物(用在 拖入 建築物)
    public IEnumerator Build_Building_Drag (RBuilding vInfo)
    {
        Ray vRay = mHome.Camera.Camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit vHit;
        int vLayerMask = ((1 << LayerMask.NameToLayer("Ground")) | (1 << LayerMask.NameToLayer("Block")));

        //
        Vector3 vPos = Vector3.zero;

        //檢查是否有偵測到地塊
        if (Physics.Raycast(mHome.Camera.transform.position, vRay.direction, out vHit, Mathf.Infinity, vLayerMask))
            vPos = vHit.point;

        //取得建築物資料
        RHomeBuildingJson vBuildingData = GameInfo.DataMgr.Data_HomeBuilding.GetDataByKey(vInfo.BuildingID);

        TObject vObj = new TObject();

        //取得建築物
        yield return GameInfo.CoroutineMgr.StartCoroutine(GetBuilding(vBuildingData, vObj));

        //檢查是否有取得
        if (vObj.Obj == null)
        {
            yield break;
        }
        else
        {
            //設置建築物位置.角度.大小
            GameObject vBuilding = vObj.Obj;
            vBuilding.transform.parent = transform;
            vBuilding.transform.position = new Vector3(vPos.x, 0.01f, vPos.y);
            vBuilding.transform.localEulerAngles = new Vector3(0, 0, 0);
            vBuilding.transform.localScale = Vector3.one / transform.localScale.x;

            TweenPosition.Begin(mHome.Panel_Storehouse, 0.1f, new Vector3(0, -255, 0));

            yield return new WaitForSeconds(0.1f);

            //掛載建築物腳本
            OHome_Building xBuilding = vBuilding.GetComponent<OHome_Building>();

            if (xBuilding == null)
                xBuilding = vBuilding.AddComponent<OHome_Building>();

            xBuilding.Home = mHome;
            xBuilding.Panel = GameObject.Instantiate(mHome.Panel_Building).transform;
            xBuilding.ID = vInfo.BuildingID;
            xBuilding.Area = (eArea)vInfo.Area;
            xBuilding.BuildingRaiseUp();

            if (mIsPress == true)
                mBuilding = xBuilding;
        }

        mHome.Camera.IsEnable = true;

        vObj.Dispose();
        vObj = null;
    }

	//建立建築物(用在 單點擊 建築物)
	public IEnumerator Build_Building_Click (RBuilding vInfo)
	{
		yield return true;
		mIsDraw = true;
		mRoadList = new List<RMapBlock>();
		Home.Camera.IsEnable = false;
	}

	//繪製道路
	private void Road_Draw ()
	{
		if(mIsDraw == false)
			return;

		Ray vRay = mHome.Camera.Camera.ScreenPointToRay(Input.mousePosition);
		RaycastHit vHit;
		int vLayerMask = ((1 << LayerMask.NameToLayer("Ground")) | (1 << LayerMask.NameToLayer("Block")));

		Vector3 vPos = Vector3.zero;

		Transform vDrawBlock;
		
		//檢查是否有偵測到地塊
		if (Physics.Raycast(mHome.Camera.transform.position, vRay.direction, out vHit, Mathf.Infinity, vLayerMask))
		{
			//取得錨點
			vDrawBlock = vHit.collider.transform;

			if (CheckBlockUsed(vDrawBlock) == false)
			{
				SetBlockUsed(vDrawBlock, true);

				Road_AddList(vDrawBlock);

				if (!CheckRoadConnect(vDrawBlock))
				{
					SetRoadConnect(vDrawBlock);
				}
				mRoad = vDrawBlock;
			}
//			vPos = vHit.point;
		}
	}

	//繪製道路 結束
	private void Road_DrawEnd ()
	{
		mIsDraw = false;
		mIsRoadEdit = true;
		mHome.Camera.IsEnable = true;
		
		Road_ShowGroup();
	}

	private void Road_Move ()
	{
		if(mIsRoadMove == false)
			return;

		Ray vRay = mHome.Camera.Camera.ScreenPointToRay(Input.mousePosition);
		RaycastHit vHit;
		int vLayerMask = ((1 << LayerMask.NameToLayer("Ground")) | (1 << LayerMask.NameToLayer("Block")));
		
		Vector3 vPos = Vector3.zero;
		
		Transform vDrawBlock;
		
		//檢查是否有偵測到地塊
		if (Physics.Raycast(mHome.Camera.transform.position, vRay.direction, out vHit, Mathf.Infinity, vLayerMask))
		{
			//取得錨點
			vDrawBlock = vHit.collider.transform;

			if (CheckRoadMove(vDrawBlock))
			{
				SetNextRoadMove(vDrawBlock);
			}
		}
	}

	//繼續增加路 vFromEnd 從起點增加
	public void Road_Extend (bool vFromStart = false)
	{
		if(vFromStart == true)
			mRoadList.Reverse();

		mRoad = mRoadList[mRoadList.Count - 1].Anchor;

		mIsDraw = true;
		mIsRoadEdit = false;
		Home.Camera.IsEnable = false;
	}

	//路面群組移動
	public void Road_MovePrapare ()
	{
		Home.Camera.IsEnable = false;

		mRoad = mRoadList[(mRoadList.Count / 2)].Anchor;

		mSimpleList = new List<RSimpleBlock>();

		for(int i = 0; i < mRoadList.Count; i++)
		{
			mSimpleList.Add(new RSimpleBlock(mRoadList[i].Row,mRoadList[i].Col));
		}

		mIsRoadMove = true;
	}

	//加到路面列表
	private void Road_AddList (Transform vTrans ,bool ToLast = true)
	{
		int vIndex1 = GetBlockIndex(vTrans, 1);
		int vIndex2 = GetBlockIndex(vTrans, 2);
		RMapBlock vRoadData = Maps[vIndex1, vIndex2];

		if(ToLast == true)
		{
			mRoadList.Add(vRoadData);
			Road_SetMaterial(mRoadList.Count - 1);
		}
		else
		{
			int vRoadCount = mRoadList.Count;
			RMapBlock vLastBlock = mRoadList[vRoadCount - 1];

			mRoadList.RemoveAt(vRoadCount - 1);
			mRoadList.Add(vRoadData);
			Road_SetMaterial(mRoadList.Count - 1);
			mRoadList.Add(vLastBlock);
			Road_SetMaterial(mRoadList.Count - 1);
		}
	}

	//貼上貼圖
	private void Road_SetMaterial (int vBlockIndex)
	{
		if(mRoadList == null)
			return;

		if(mRoadList.Count == 0)
			return;

		RSimpleBlock vNowBlock = new RSimpleBlock(mRoadList[vBlockIndex].Row, mRoadList[vBlockIndex].Col);

		byte vMatCheck = 0;
		RSimpleBlock[] vBlockAround = GetRoadConnection(vNowBlock, out vMatCheck);

		Debug.LogWarning(vBlockIndex + ":" + vMatCheck);
		//當前點
		SetBlockMat(Maps[vNowBlock.Row, vNowBlock.Col].RenderMat, vMatCheck);
		
		//4周點
		SetGroupBlockMat(vBlockAround);
	}

	private void Road_ShowGroup ()
	{
		int vGroupCount = mRoadList.Count;

		if(vGroupCount == 0)
			return;

		Vector3 vCenterPos = mRoadList[(vGroupCount / 2)].Anchor.transform.position;
		Vector3 vStartPos = mRoadList[0].Anchor.transform.position;
		Vector3 vEndPos = mRoadList[(vGroupCount - 1)].Anchor.transform.position;
		
		Home.SetRoadGroup(vCenterPos, vStartPos, vEndPos);
		
		//設置建築物位置.角度.大小
//		mExtendPoint[0].transform.position = new Vector3(vStartPos.x, 0.01f, vStartPos.z);
//		mExtendPoint[1].transform.position = new Vector3(vEndPos.x, 0.01f, vEndPos.z);
	}
	
    //顯示地塊
    public void ShowBlock ()
    {
        if (mBlockRenderers == null)
            return;

        //開啟地塊渲染
        for (int i = 0; i < mBlockRenderers.Length; i++)
            mBlockRenderers[i].enabled = true;
    }

    //隱藏地塊
    public void HideBlock ()
    {
        if (mBlockRenderers == null)
            return;

        //關閉地塊渲染
        for (int i = 0; i < mBlockRenderers.Length; i++)
            mBlockRenderers[i].enabled = false;
    }
}