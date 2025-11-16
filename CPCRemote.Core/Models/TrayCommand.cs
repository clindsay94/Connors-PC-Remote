using CPCRemote.Core.Enums;

namespace CPCRemote.Core.Models;

public
sealed class TrayCommand {
 public
  TrayCommandType CommandType {
    get;
    set;
  }
 public
  string Name {
    get;
    set;
  }
  = string.Empty;
}
