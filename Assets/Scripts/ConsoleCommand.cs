public abstract class ConsoleCommand {
    public readonly string name;
    public readonly string description;
    public virtual bool IsVariable => false;
    public abstract object Execute(World world);
    
    protected ConsoleCommand(string name, string description=null) {
        this.name = name;
        this.description = description;
    }
}