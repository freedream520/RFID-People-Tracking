%% 
% this function is used to generate people tracking and identification results with a GIF image
% Arthur, 2016 Spring
%%

close all
clear all
clc

%% Load file and establish timeline
dirctory=pwd;
file='\2-8-2016_21-27-06.txt';
filename=strcat(dirctory,file);
fprintf('Now working on file: %s \n',filename);

fid = fopen(filename);
info = textscan(fid, '%s %s %f %f ','delimiter',',');
fclose(fid);


id=info{1};
time=info{2};
xCoordinate=info{3};
yCoordinate=info{4};

peopleID=str2Double(id);
[positionStartTime,timeInSecond]=time2Second(time);
timeInSecond=timeInSecond+1;
uniqueID=unique(peopleID);
timeMax=max(timeInSecond);
peopleLocation=cat(3,zeros(timeMax,length(uniqueID)),zeros(timeMax,length(uniqueID)));

minX=-50;
maxX=650;
minY=0;
maxY=500;

% setupp color for people
colorMap=['c','m','b','y','r','g','w'];
colorForPeople=zeros(1,7);

% Generate people's location in single second
for t=1:timeMax
    for p=1:length(uniqueID)
        index=find(peopleID==uniqueID(p) & timeInSecond==t);
        if (~isempty(index))
            peopleLocation(t,p,1)=mean(xCoordinate(index));
            peopleLocation(t,p,2)=mean(yCoordinate(index));
        end
    end  
end

% Generate Frames and use differnt color for different ID
count=1;
filename = 'trackingAndIdentification.gif';

for t=1:timeMax
   figureToPlot=figure(1);
   
   % Reset the unused colors
   if (t>1)
       resetIndex=find(peopleLocation(t-1,:,1)~=0 & peopleLocation(t,:,1)==0);
       if (~isempty(resetIndex))
           for tmp=1:length(resetIndex)
                colorForPeople(find(colorForPeople==resetIndex(tmp)))=0;
           end
       end
   end
   
%    rectangle('Position',[-100 205 130 70],'FaceColor','y');
%    axis([minX,maxX,minY,maxY]);
   indexP=find(peopleLocation(t,:,1));
   axis([minX,maxX,minY,maxY]);
   for p=1:length(indexP)
       % find a usable color if there is no color assigned to the people
       if(isempty(find(colorForPeople==indexP(p))))
            indexColorNotUsed=find(colorForPeople==0);
            colorForPeople(indexColorNotUsed(1))=indexP(p);
       end
       
       % Plot figure with given color
       colorIndex=find(colorForPeople==indexP(p));
       cmd=strcat('o',colorMap(colorIndex));
       xtmp=peopleLocation(t,indexP(p),1);
       ytmp=abs(peopleLocation(t,indexP(p),2));
       
       plot(xtmp,ytmp,cmd,'MarkerFaceColor',colorMap(colorIndex),'MarkerSize',10); hold on
      if (p==1 && t>5)
            xRec=xtmp;
            yRec=ytmp;
            str1 = '\leftarrow Arthur';
            text(xRec,yRec,str1)
       end
       axis([minX,maxX,minY,maxY]);
   end
   
   imagePath=strcat(pwd,'\tmp.jpg');
   saveas(figureToPlot,imagePath)
   fprintf('Frame %d is written into file \n',t);
   clf(figureToPlot,'reset');
   
   % write current frame to video
   frameCurrent=imread(imagePath);
%    if (t>5)
%        frameCurrent = insertText(frameCurrent, [xtmp, 500-ytmp], 'Arthur');
%    end
   [imind,cm] = rgb2ind(frameCurrent,256);
      
    if (t == 1)
      imwrite(imind,cm,filename,'gif', 'Loopcount',inf);
    else
      imwrite(imind,cm,filename,'gif','WriteMode','append');
    end
end

