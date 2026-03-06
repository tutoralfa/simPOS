using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace simPOS.Shared.Reports
{
    public class ProfitLossService
    {
        private readonly ProfitLossRepository _repo = new ProfitLossRepository();

        public List<ProfitLossRow> GetRows(ProfitLossFilter f)
            => _repo.GetProfitLoss(f);

        public ProfitLossSummary GetSummary(ProfitLossFilter f)
            => _repo.GetSummary(f);
    }
}
