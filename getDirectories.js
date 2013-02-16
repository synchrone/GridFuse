db.CollectionToOutputResultsTo.drop();

db.fs.files.mapReduce(
    function(){
        var regex = /^C:\\([^\\]+)\\/;
        var match = regex.exec(this.filename);

        if(match != null && match.length > 1)
        {
            print("MATCH: "+this.filename+"  "+regex);
            emit(match[1],this._id);
        }else{
            print("NOT A MATCH: "+this.filename+" does not match "+regex);
        }
    },
    function(){}, // not gonna reduce, as it does not get applied to a one-valued key
    {
        out: "CollectionToOutputResultsTo",
        finalize: function(key,values){
            return key;
        }
    }
);