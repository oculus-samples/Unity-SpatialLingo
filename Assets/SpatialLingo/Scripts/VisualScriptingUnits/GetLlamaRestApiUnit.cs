// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.Utilities.LlamaAPI;
using Meta.XR.Samples;
using Unity.VisualScripting;

namespace SpatialLingo.VisualScriptingUnits
{
    [MetaCodeSample("SpatialLingo")]
    public class GetLlamaRestApiUnit : Unit
    {

        [DoNotSerialize] private ValueOutput m_llamaRestApi;
        private LlamaRestApi m_apiInst;

        protected override void Definition()
        {
            m_llamaRestApi = ValueOutput(nameof(m_llamaRestApi), GetLlamaRestApi);
        }

        private LlamaRestApi GetLlamaRestApi(Flow flow)
        {
            return m_apiInst ??= new LlamaRestApi();
        }
    }
}
