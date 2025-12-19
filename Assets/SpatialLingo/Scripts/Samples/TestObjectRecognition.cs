// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using TMPro;
using Unity.InferenceEngine;
using UnityEngine;

namespace SpatialLingo.Samples
{
    /// <summary>
    /// This script shows an example of object recognition
    /// The result is an overlay of identified objects on an input image 
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class TestObjectRecognition : MonoBehaviour
    {
        // Static model input dimensions:
        private const int YOLO_MODEL_IMAGE_WIDTH = 640;
        private const int YOLO_MODEL_IMAGE_HEIGHT = 640;

        // Source model onnx file (eg: yolo12s)
        [Tooltip("Drag a yolo onnx model file here")]
        [SerializeReference] public ModelAsset ModelYolo;

        // Text file with newlines separating the list of recognized object classes
        [Tooltip("Drag the classes.txt here")]
        [SerializeReference] public TextAsset ModelClasses;

        // Image to test with, should be dimensions model can handle (eg: 640x640)
        [Tooltip("Drag a image source here")]
        [SerializeReference] public Texture2D ImageSource;

        // Mesh to display the source image 
        [Tooltip("Drag a plane from the scene to display the image and result")]
        [SerializeReference] public MeshRenderer DisplayMesh;

        // Cube to overlay 
        [Tooltip("Drag a cube overlay object/prefab to display debug found object rectangles")]
        [SerializeReference] public GameObject DisplayRectangle;

        // Cube to overlay 
        [Tooltip("Drag a TMP Display Text field parent for class labeling")]
        [SerializeReference] public GameObject DisplayClass;

        private Worker m_worker;
        private string[] m_labels;

        private Tensor<float> m_centersToCorners = new(new TensorShape(4, 4),
        new float[]
        {
            1, 0, 1, 0,
            0, 1, 0, 1,
            -0.5f, 0, 0.5f, 0,
            0, -0.5f, 0, 0.5f
        });

        /// <summary>
        /// Pass in a classic yolo onnx model asset, convert to easily-useable formatted worker
        /// Derived from https://huggingface.co/unity/inference-engine-yolo/blob/main/RunYOLO.cs
        /// </summary>
        /// <param name="yoloModel"></param>
        private static Worker YoloModelToSentisWorker(ModelAsset yoloModel, Tensor<float> localTranform)
        {
            // Settings
            var iouThreshold = 0.5f; // Intersection over union threshold used for non-maximum suppression [0,1]
            var scoreThreshold = 0.5f; // Confidence score threshold used for non-maximum suppression [0,1]
            const BackendType BACKEND = BackendType.GPUCompute;

            // Load model
            var sentisModel = ModelLoader.Load(yoloModel);

            // Here we transform the output of the model by feeding it through a Non-Max-Suppression layer.
            var graph = new FunctionalGraph();
            var inputs = graph.AddInputs(sentisModel);
            var modelOutput = Functional.Forward(sentisModel, inputs)[0]; //shape=(1,84,8400)
            var boxCoords = modelOutput[0, 0..4, ..].Transpose(0, 1); //shape=(8400,4)
            var allScores = modelOutput[0, 4.., ..]; //shape=(80,8400)
            var scores = Functional.ReduceMax(allScores, 0); //shape=(8400)
            var classIDs = Functional.ArgMax(allScores, 0); //shape=(8400)
            var boxCorners = Functional.MatMul(boxCoords, Functional.Constant(localTranform)); //shape=(8400,4)
            var indices = Functional.NMS(boxCorners, scores, iouThreshold, scoreThreshold); //shape=(N)
            var coords = Functional.IndexSelect(boxCoords, 0, indices); //shape=(N,4)
            var labelIDs = Functional.IndexSelect(classIDs, 0, indices); //shape=(N)

            //Create worker to run model
            var worker = new Worker(graph.Compile(coords, labelIDs), BACKEND);
            return worker;
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            // Set the display mesh to use the input image source
            DisplayMesh.material.mainTexture = ImageSource;
            // Parse class labels
            m_labels = ModelClasses.text.Split('\n');
            // Create worker for doing operations
            m_worker = YoloModelToSentisWorker(ModelYolo, m_centersToCorners);
            // Start work
            ExecuteMl();
        }

        private void ExecuteMl()
        {
            var targetRT = ImageSource;
            var inputTensor = new Tensor<float>(new TensorShape(1, 3, YOLO_MODEL_IMAGE_HEIGHT, YOLO_MODEL_IMAGE_WIDTH));
            TextureConverter.ToTensor(targetRT, inputTensor, default);
            m_worker.Schedule(inputTensor);

            using var output = (m_worker.PeekOutput("output_0") as Tensor<float>).ReadbackAndClone();
            using var labelIDs = (m_worker.PeekOutput("output_1") as Tensor<int>).ReadbackAndClone();

            var boxesFound = output.shape[0];

            // Display plane is expected to be 1 unit length
            var displayWidth = 1.0f;
            var displayHeight = 1.0f;

            // Scale the square to a box of expected size
            var scaleX = displayWidth / YOLO_MODEL_IMAGE_WIDTH;
            var scaleY = displayHeight / YOLO_MODEL_IMAGE_HEIGHT;

            //Draw the bounding boxes
            for (var n = 0; n < Mathf.Min(boxesFound, 200); n++)
            {
                var box = Instantiate(DisplayRectangle, DisplayMesh.transform);
                box.transform.rotation = Quaternion.identity;

                var centerX = output[n, 0] * scaleX - displayWidth / 2;
                var centerY = output[n, 1] * scaleY - displayHeight / 2;
                var width = output[n, 2] * scaleX;
                var height = output[n, 3] * scaleY;
                box.transform.localScale = new Vector3(width, height, 1.0f);
                box.transform.localPosition = new Vector3(centerX, -centerY, -0.01f);
                var label = m_labels[labelIDs[n]];

                var display = Instantiate(DisplayClass, DisplayMesh.transform);
                display.transform.localPosition = new Vector3(centerX, -centerY, -0.02f);
                display.transform.rotation = box.transform.rotation;

                var text = display.GetComponentInChildren<TextMeshPro>();
                if (text != null)
                {
                    text.text = label;
                }
            }
        }

        private void OnDestroy()
        {
            m_centersToCorners?.Dispose();
            m_worker?.Dispose();
        }
    }
}
