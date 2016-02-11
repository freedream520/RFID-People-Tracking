%% 
% this code is used to matchig RFID distance and Kinect distance

%%
clear all
close all
clc

%%
% load position data
    fileCode='2-8-2016_22-55-44';
    positionFilename=strcat('\',fileCode,'.txt');
    fileID = fopen(positionFilename); 
    info=textscan(fileID,'%s %s %f %f','Delimiter',',');
    fclose(fileID);
    
    xCoordinate=info{3};
    yCoordinate=info{4};
    
    peopleID=str2Double(info{1});
    [kinectStartTime,kinectTime]=time2Second(info{2});
    maxTime=max(kinectTime);
    numberOfSkeleton=max(peopleID);
    distanceTmp=zeros(numberOfSkeleton,maxTime);
    
    for i=1:maxTime   
        for j=1:numberOfSkeleton
            index=find(kinectTime==i & peopleID==j);
            if (index~=0)
             distanceTmp(j,i)=pdist([450,0;(xCoordinate(index(length(index)))),(yCoordinate(index(length(index))))],'euclidean');
            else
                distanceTmp(j,i)=0;
            end
         clear index;
        end
    end
    
    for i=1:numberOfSkeleton
%         tmp1=[distanceTmp(i,2:size(distanceTmp,2)),0];
%         tmp2=tmp1-distanceTmp(i,:);
%         tmp2(size(distanceTmp,2))=0;
        relativeDistance(i,:)=smooth(gradient(distanceTmp(i,:))'./100,5);
        
        index=find(relativeDistance(i,:)>=5);
        relativeDistance(i,index)=5;
        clear index;      
        
        index=find(relativeDistance(i,:)<=-5);
        relativeDistance(i,index)=-5;
        clear index;
    end
    relativeDistance=relativeDistance.*(-1);
    
% load RFID data
    positionFilename=strcat('\RFID_',fileCode,'.txt');
    fileID = fopen(positionFilename); 
    info=textscan(fileID,'%s %f %f %f %s','Delimiter',',');
    fclose(fileID);
    
    tagID=info{1};
    estiamtedDFS=smooth(info{2},5);
    apiDFS=smooth(info{3},5);
    speed=smooth(info{4},5);
    [rfidStartTime,rfidTime]=time2MS(info{5});
    
    % sync time
    offset=kinectStartTime-rfidStartTime;
    rfidTime=rfidTime-offset*1000;
   
    for i=1:max(ceil(rfidTime./1000))   
        index=find(rfidTime>=(i-1)*1000+1 & rfidTime<i*1000);
        temp=0;
        for it=1:length(index)
            if (it==1)
                temp=temp+speed(index(it))*(rfidTime(index(it))-(i-1)*1000)/1000;
            elseif (it<=length(index))
                temp=temp+speed(index(it))*(rfidTime(index(it))-rfidTime(index(it-1)))./1000;
            end         
        end
        
        if (~isempty(index) && i<max(ceil(rfidTime./1000)))
            temp=temp+speed(index(it)+1)*(i*1000-rfidTime(index(it)))./1000;
        elseif (~isempty(index) && i==max(ceil(rfidTime./1000)))
            temp=temp+speed(index(it))*(i*1000-rfidTime(index(it)))./1000;            
        end
        rfidReletiveDistance(i)=temp;
        clear index
    end
    
%     if (offset>=0)
%         rfidReletiveDistance=[zeros(offset,1);rfidReletiveDistance'];
%     else
%         rfidReletiveDistance(1:offset)=[];
%     end

    duration=min(length(rfidReletiveDistance),size(relativeDistance,2));
    
    figure
    plot(rfidReletiveDistance(1:duration),'rx-'); hold on;
    for i=1:size(relativeDistance,1)
        plot(relativeDistance(i,1:duration)); hold on;
    end
    
    