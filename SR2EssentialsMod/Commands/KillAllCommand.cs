﻿namespace SR2E.Commands;

internal class KillAllCommand : SR2CCommand
{
    public override string ID => "killall";

    public override string Usage => "killall [id]";

    public override string Description => "Kills everything";
    public override List<string> GetAutoComplete(int argIndex, string[] args)
    {
        if (argIndex == 0)
        {
            string firstArg = "";
            if (args != null)
                firstArg = args[0];
            List<string> list = new List<string>();
            int i = -1;
            foreach (IdentifiableType type in SR2EEntryPoint.identifiableTypes)
            {
                if (i > 55)
                    break;
                try
                {
                    if (type.LocalizedName != null)
                    {
                        string localizedString = type.LocalizedName.GetLocalizedString();
                        if (localizedString.ToLower().Replace(" ", "").StartsWith(firstArg.ToLower()))
                        {
                            i++;
                            list.Add(localizedString.Replace(" ", ""));
                        }
                    }
                }
                catch { }

            }

            return list;
        }

        return null;
    }
    public override bool Execute(string[] args)
    {
        if (args == null)
        {
            foreach (var ident in Resources.FindObjectsOfTypeAll<IdentifiableActor>())
            {
                if (ident.hasStarted)
                {
                    var id = ident.model.actorId;
                    if (ident.identType.name != "Player")
                    {
                        Object.Destroy(ident.gameObject);
                        SceneContext.Instance.GameModel.identifiables.Remove(id);
                    }
                }
            }
            return true;
        }
        if (args.Length == 1)
        {
            foreach (var ident in Resources.FindObjectsOfTypeAll<IdentifiableActor>())
            {
                if (ident.hasStarted)
                {
                    if (ident.identType == SR2EEntryPoint.getIdentifiableByLocalizedName(args[0]))
                    {
                        var id = ident.model.actorId;
                        Object.Destroy(ident.gameObject);
                        SceneContext.Instance.GameModel.identifiables.Remove(id);
                    }
                    else if (ident.identType == SR2EEntryPoint.getIdentifiableByName(args[0]))
                    {
                        var id = ident.model.actorId;
                        Object.Destroy(ident.gameObject);
                        SceneContext.Instance.GameModel.identifiables.Remove(id);
                    }
                }
            }
            return true;
        }
        return false;
    }
}
