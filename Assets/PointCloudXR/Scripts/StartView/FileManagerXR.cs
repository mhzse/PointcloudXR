/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using Valve.VR.InteractionSystem;

public enum ImportMode { NONE, IMPORT_TXT, IMPORT_LAS };
public class FileManagerXR : MonoBehaviour
{
    public  ViveCtrl           _ViveCtrl;
    public  PointCloudManager  _PointCloudManager;

    public GameObject _ButtonPrefab;
    public GameObject _MenuPointerPrefab;

    private Hand       _RightHand;
    public  Material   _MenuPointerMaterial;

    public GameObject _FileLists;

    public GameObject _LoadContainer;
    public GameObject _ImportLASContainer;
    public GameObject _ImportTXTContainer;

    private FileInfo[] _FilesImportLAS;
    private FileInfo[] _FilesImportTXT;
    private FileInfo[] _FilesSerialized;

    private List<GameObject> _ImportLASButtons;
    private List<GameObject> _LoadButtons;

    private GameObject _MenuPointer;

    private string _ReadFileName = string.Empty;

    private StartMode  _StartMode  = StartMode.NONE;
    private ImportMode _ImportMode = ImportMode.NONE;

    void Start()
    {
        ClearSelectedFileName();
        UpdateFiles();

        if (_MenuPointer == null)
        {
            InstantiateMenuPointer();
        }
    }

    public StartMode GetStartMode()
    {
        return _StartMode;
    }

    private void InstantiateMenuPointer()
    {
        if(_RightHand == null)
        {
            _RightHand = _ViveCtrl.GetRightHand();
        }

        _MenuPointer = Instantiate(_MenuPointerPrefab);
        _MenuPointer.name = "MenuPointer_FileManager";

        GameObject pointerBase = new GameObject();
        pointerBase.transform.parent = _RightHand.transform;
        pointerBase.transform.localPosition = new Vector3(0.0f, -0.087f, 0.063f);
        pointerBase.transform.localRotation = Quaternion.Euler(60.0f, 0.0f, 0.0f);
        pointerBase.name = "pointerBase";

        _MenuPointer.transform.parent = pointerBase.transform;

        _MenuPointer.transform.localScale = new Vector3(0.01f, 0.01f, 0.03f);
        _MenuPointer.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
        _MenuPointer.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

        _MenuPointer.AddComponent<MenuPointerCtrl>();
        _MenuPointer.GetComponent<Renderer>().material = _MenuPointerMaterial;
        _MenuPointer.AddComponent<BoxCollider>();
        _MenuPointer.GetComponent<BoxCollider>().isTrigger = true;
        _MenuPointer.GetComponent<BoxCollider>().center = new Vector3(0.0104362f, 0.0007376721f, 0.5f);
        _MenuPointer.GetComponent<BoxCollider>().size = new Vector3(0.14f, 0.12f, 1f);
        _MenuPointer.SetActive(true);
    }

    public ImportMode GetImportMode()
    {
        return _ImportMode;
    }

    private void ClearSelectedFileName()
    {
        if (_PointCloudManager != null)
        {
            _PointCloudManager.ReadFilenameSet(string.Empty);
            _PointCloudManager.WriteFilenameSet(string.Empty);
        }

        _StartMode = StartMode.NONE;
    }

    public void SetComponentsActive( bool active )
    {
        if (_MenuPointer == null)
        {
            InstantiateMenuPointer();
        }

        _MenuPointer.SetActive(active);
        _FileLists.SetActive(active);

        if(active)
        {
            ClearSelectedFileName();
            UpdateFiles();
        }
    }

    void OnLASFileSelect(object sender, ButtonToggleEvent e)
    {
        if(e._Active)
        {
            _StartMode = StartMode.IMPORT;
            _PointCloudManager.ReadFilenameSet(e._Name.Split('.')[0]);
            _ImportMode = ImportMode.IMPORT_LAS;
            foreach (GameObject button in _ImportLASButtons)
            {
                if (button.name != e._Name)
                {
                    button.GetComponent<ButtonXR>().SetActive(false);
                }
            }
            SetLoadFileButtonsActive(false);
        }
        else
        {
            _PointCloudManager.ReadFilenameSet("");
            _StartMode = StartMode.NONE;
        }
    }

    private void SetLASFileButtonsActive( bool active )
    {
        for( int i = 0; i < _ImportLASButtons.Count; i++ )
        {
            _ImportLASButtons[i].GetComponent<ButtonXR>().SetActive(active);
        }
    }

    void OnLoadFileSelect(object sender, ButtonToggleEvent e)
    {
        if (e._Active)
        {
            _StartMode = StartMode.LOAD;
            _PointCloudManager.ReadFilenameSet(e._Name.Split('.')[0]);
            foreach (GameObject button in _LoadButtons)
            {
                if (button.name != e._Name)
                {
                    button.GetComponent<ButtonXR>().SetActive(false);
                }
            }
            SetLASFileButtonsActive(false);
        }
        else
        {
            _PointCloudManager.ReadFilenameSet("");
            _StartMode = StartMode.NONE;
        }
    }

    private void SetLoadFileButtonsActive(bool active)
    {
        for (int i = 0; i < _LoadButtons.Count; i++)
        {
            _LoadButtons[i].GetComponent<ButtonXR>().SetActive(active);
        }
    }

    private void UpdateFiles()
    {
        _ReadFileName = _PointCloudManager.ReadFilenameGet();

        DestroyChildren(_LoadContainer);
        DestroyChildren(_ImportLASContainer);
        DestroyChildren(_ImportTXTContainer);

        _FilesImportLAS = _PointCloudManager.FilesImportLASGet();
        _FilesImportTXT = _PointCloudManager.FilesImportTXTGet();
        _FilesSerialized = _PointCloudManager.FilesSerializedGet();

        _ImportLASButtons = CreateFilenameButtonsList(_FilesImportLAS, _ImportLASContainer);
        for( int i = 0; i < _ImportLASButtons.Count; i++ )
        {
            _ImportLASButtons[i].GetComponent<ButtonXR>().OnToggleEvent += OnLASFileSelect;
        }

        _LoadButtons = CreateFilenameButtonsList(_FilesSerialized, _LoadContainer);
        for (int i = 0; i < _LoadButtons.Count; i++)
        {
            _LoadButtons[i].GetComponent<ButtonXR>().OnToggleEvent += OnLoadFileSelect;
        }
    }

    private List<GameObject> CreateFilenameButtonsList(FileInfo[] files, GameObject container)
    {
        List<GameObject> list = new List<GameObject>();

        for (int i = 0; i < files.Length; i++)
        {
            GameObject button = Instantiate(_ButtonPrefab);

            ButtonXR bxr = button.GetComponent<ButtonXR>();
            bxr.GetText().gameObject.transform.parent = button.transform;
            bxr.GetFront().gameObject.transform.parent = button.transform;
            bxr.GetBase().gameObject.transform.parent = button.transform;

            button.name                       = files[i].Name;
            button.transform.parent           = container.transform;
            button.transform.localPosition    = new Vector3(0.0f, -i * 0.022f, 0.0f);
            button.transform.localEulerAngles = new Vector3(0, 0, 0);

            string buttonText    = files[i].Name.Split('.')[0];
            float  scaleFactor   = 0.12f;
            int    numberOfChars = buttonText.Length;
            float  newScaleX     = scaleFactor * numberOfChars;

            float textWidthFactor = 0.007f;

            TextMeshPro textMesh        = button.GetComponent<ButtonXR>().GetTextMeshPro();
            RectTransform rectTransform = textMesh.rectTransform;
            rectTransform.sizeDelta     = new Vector2(textWidthFactor * numberOfChars, 0.035f * 0.4f);

            button.GetComponent<ButtonXR>().SetScaleY(0.5f);
            button.GetComponent<ButtonXR>().SetScaleX(newScaleX);

            button.GetComponent<ButtonXR>().SetText(buttonText);

            bxr.GetText().gameObject.transform.parent        = bxr.GetFront().gameObject.transform;
            bxr.GetText().gameObject.transform.localPosition = new Vector3(0, 0.008f, 0.0018f);
            bxr.GetFront().gameObject.transform.parent       = bxr.GetBase().gameObject.transform;
            
            list.Add(button);
        }

        return list;
    }

    private void DestroyChildren(GameObject go)
    {
        foreach (Transform child in go.transform)
        {
            Destroy(child.gameObject);
        }
    }
}
