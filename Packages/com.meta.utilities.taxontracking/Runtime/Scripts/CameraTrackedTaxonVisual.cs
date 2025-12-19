// Copyright (c) Meta Platforms, Inc. and affiliates.
using TMPro;
using UnityEngine;

namespace Meta.Utilities.CameraTaxonTracking
{
    /// <summary>
    /// This class visualizes a tracked object with a: center, extent, & label
    /// </summary>
    public class CameraTrackedTaxonVisual : MonoBehaviour
    {
        [SerializeReference] public TextMeshPro TaxonLabel;
        [SerializeReference] public GameObject TaxonCenter;
        [SerializeReference] public GameObject TaxonExtent;

        public CameraTrackedTaxon Taxon { get; private set; }

        public void SetTaxon(CameraTrackedTaxon taxon)
        {
            Taxon = taxon;
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (Taxon == null)
            {
                return;
            }
            var center = Taxon.Center;
            var extent = Taxon.Extent;
            var scale = new Vector3(extent.x, extent.y, extent.z);
            TaxonCenter.transform.position = center;
            TaxonExtent.transform.position = center;
            TaxonExtent.transform.localScale = scale;
            TaxonLabel.transform.position = center + new Vector3(0.0f, extent.y * 0.5f + 0.1f, 0.0f);
            TaxonLabel.text = Taxon.Name;
        }
    }
}
