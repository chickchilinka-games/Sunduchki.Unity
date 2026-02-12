using System.Collections.Generic;
using Features.PlayerHandSystemImpl.ViewModel;

namespace Features.PlayerHandSystemImpl.Interfaces
{
    public interface IRankStackViewModelFactory
    {
        RankStackViewModel Create(string rank, IEnumerable<string> suits);
    }
}
