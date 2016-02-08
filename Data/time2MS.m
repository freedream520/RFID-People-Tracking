%% fuction to convert time into seconds
function [startTime,timeInSecond]=time2Second(time)
    timeInSecond=zeros(length(time),1);

    strStartTime=time{1};
    Seg = strsplit(strStartTime);
    Seg = strsplit(Seg{2},':');
    startTime=str2double(Seg{1})*3600+str2double(Seg{2})*60+str2double(Seg{3});
    startTimeTmp=(str2double(Seg{1})*3600+str2double(Seg{2})*60+str2double(Seg{3}))*1000+str2double(Seg{4});
    
    for t=1:length(time)
        Seg = strsplit(time{t});
        Seg = strsplit(Seg{2},':');
        timeInSecond(t)=(str2double(Seg{1})*3600+str2double(Seg{2})*60+str2double(Seg{3}))*1000+str2double(Seg{4})-startTimeTmp+1;  
    end
return 