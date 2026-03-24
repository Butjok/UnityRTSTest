using System.Collections.Generic;
using System.Linq;

public class SelectablesRegistry : ObjectRegistry<ISelectable> {
    public override IEnumerable<ISelectable> Entities => base.Entities.Where(s => !s.IgnoreSelection);
}