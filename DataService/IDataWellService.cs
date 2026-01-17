using KindomDataAPIServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tet.Transport.Protobuf.Metaobjs;
using Tet.Transport.Protobuf.Well;

namespace KindomDataAPIServer.DataService
{
    public interface IDataWellService
    {
        Task<List<WellExport>> GetWells();
        Task<string> GetUserInfos();
        Task<List<UnitType>> get_sys_unit();

        Task<List<UnitType>> get_sys_unit_by_measureid(List<int> typeIDs);
        Task<WellOperationResult> batch_create_well_header(WellDataRequest wellDataRequest);

        Task<WellOperationResult> batch_create_well_trajectory_with_meta_infos(WellTrajRequest welltrajDataRequest);

        Task<WellOperationResult> batch_create_well_production_with_meta_infos(WellProductionDataRequest wellDataRequest);

         Task<WellOperationResult> batch_create_well_oil_test_with_meta_infos(WellOilTestDataRequset wellDataRequest);
        Task<WellOperationResult> batch_create_well_gas_pressure_test_with_meta_infos(WellGasTestRequest wellDataRequest);

        Task<List<LogSetInfo>> get_dataset_list(string datasetType = "continuous");
        Task<string> create_well_log_set(string datasetName);

        Task<PbViewMetaObjectList> get_all_meta_objects_by_objecttype_in_protobuf(string[] request);


        Task<WellOperationResult> batch_create_well_formation(PbWellFormationList pbWellFormationList);
        Task<WellOperationResult> batch_create_well_log(PbWellLogCreateList pbWellLogCreateList);

        Task<WellOperationResult> batch_create_well_payzone_with_meta_infos(CreatePayzoneRequest wellDataRequest);
        Task<WellOperationResult> batch_create_well_lithology_with_meta_infos(CreatePayzoneRequest wellDataRequest);

        Task<WellOperationResult> batch_create_well_facies_with_meta_infos(CreatePayzoneRequest wellDataRequest);
    }
}
