codeunit 99999 "ALBuild TestRunner"
{
    TestIsolation = Codeunit;
    Subtype = TestRunner;
    procedure runcodeunit(no: Integer): Boolean
    begin
        Codeunit.run(no);
        if GetLastErrorText() <> '' then
            error(GetLastErrorText() + ' ' + GetLastErrorCallStack());
        exit(true);
    end;
}